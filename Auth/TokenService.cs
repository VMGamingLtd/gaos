#pragma warning disable 8600, 8602, 8603 

using System.Security.Cryptography;
using Jose;
using Serilog;
using Gaos.Dbo;
using MySqlConnector;

namespace Gaos.Auth
{
    /*
    public enum UserType
    {
        RegisteredUser,
        GuestUser,
    }

    public class TokenClaims
    {
        public string sub;
        public long exp;

        public UserType userType;

        public int deviceId;
    }
    */

    public class TokenService
    {
        public static string CLASS_NAME = typeof(TokenService).Name;

        private static RSA? privateKey = null;
        private static RSA? publicKey = null;
        private IConfiguration Configuration;
        private string pkcs12KeyStoreFilePath;
        private static string keyStorePassword = null;

        //private Db db;
        private MySqlDataSource dataSource; 

        public TokenService(IConfiguration configuration, MySqlDataSource dataSource)
        {
            this.Configuration = configuration;
            //this.db = db;
            this.dataSource = dataSource;

            if (Configuration["pkcs12_key_store_file_path"] == null)
            {
                throw new Exception("missing configuration value: pkcs12_key_store_file_path");
            }
            this.pkcs12KeyStoreFilePath = Configuration.GetValue<string>("pkcs12_key_store_file_path");

            if (Configuration["pkcs12_key_store_file_path"] == null)
            {
                throw new Exception("missing configuration value: pkcs12_key_store_file_path");
            }
            
            string keyStorePasswordEncrypted = Configuration.GetValue<string>("key_store_password");
            if (keyStorePassword == null)
            {
                keyStorePassword = Gaos.Encryption.EncryptionHelper.Decrypt(keyStorePasswordEncrypted);
            }
        }

        private string GetPkcs12KeyStoreFilePath()
        {
            return pkcs12KeyStoreFilePath;
        }

        private string GetKeyStorePassword()
        {
            return keyStorePassword;
        }

        private string GenerateJWT(RSA privateKey, string username, int userId, int deviceId, long validitySeconds, Gaos.Model.Token.UserType userType)  
        {

            // Set JWT payload.
            var payload = new Dictionary<string, object>
            {
                { "sub", username },
                { "exp", validitySeconds },
                { "user_id", userId },
                { "user_type", userType.ToString()},
                { "device_id", deviceId}
            };
            long exp = (long)payload["exp"];

            // Create and sign the JWT.
            string jwt = Jose.JWT.Encode(payload, privateKey, JwsAlgorithm.RS256);

            return jwt;
        }

        public string GenerateJWT(string username, int userId, int deviceId, long validitySeconds,  Gaos.Model.Token.UserType userType)
        {
            const string METHOD_NAME = "GenerateJWT()";
            string jwtStr;

            // Remove all tokens for the device.
            /*
            db.JWT.RemoveRange(db.JWT.Where(t => t.DeviceId == deviceId));
            db.SaveChanges();
            */

            if (privateKey == null) { 
                privateKey = RSAKeys.ReadPrivateKey(GetPkcs12KeyStoreFilePath(), GetKeyStorePassword());
                jwtStr = GenerateJWT(privateKey, username, userId, deviceId, validitySeconds, userType);
            } 
            else
            {
                jwtStr =  GenerateJWT(privateKey, username, userId, deviceId, validitySeconds, userType);

            }
            
            var currentTime = DateTime.Now;

            if (userType == Gaos.Model.Token.UserType.RegisteredUser)
            {
                {
                    using var connection = dataSource.OpenConnection();

                    using var command_rm = connection.CreateCommand();
                    command_rm.CommandText = "DELETE FROM Jwt WHERE DeviceId = @deviceId";
                    command_rm.Parameters.AddWithValue("@deviceId", deviceId);
                    command_rm.ExecuteNonQuery();


                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Jwt (Token, UserId, DeviceId, CreatedAt, ExpiresAt)
                        VALUES (@token, @userId, @deviceId, NOW(), DATE_ADD(NOW(), INTERVAL @validitySeconds SECOND));
                        SELECT LAST_INSERT_ID();";

                    command.Parameters.AddWithValue("@token", jwtStr);
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@deviceId", deviceId);
                    command.Parameters.AddWithValue("@validitySeconds", validitySeconds);
                    
                    var result = command.ExecuteScalar();
                    int jwtId;
                    if (result == null)
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME} Could not insert JWT into database: result is null");
                        throw new Exception("Could not insert JWT into database: result is null");
                    }
                    else
                    if (!int.TryParse(result.ToString(), out jwtId))
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME} Could not insert JWT into database: could not parse result to int");
                        throw new Exception("Could not insert JWT into database: could not parse result to int");
                    }
                }


            }
            else if (userType == Gaos.Model.Token.UserType.GuestUser)
            {
                /*
                Gaos.Dbo.Model.JWT jwt = new Gaos.Dbo.Model.JWT
                {
                    Token = jwtStr,
                    UserId = userId,
                    DeviceId = deviceId,
                };
                db.JWT.Add(jwt);
                db.SaveChanges();
                */

                {
                    using var connection = dataSource.OpenConnection();

                    using var command_rm = connection.CreateCommand();
                    command_rm.CommandText = "DELETE FROM Jwt WHERE DeviceId = @deviceId";
                    command_rm.Parameters.AddWithValue("@deviceId", deviceId);
                    command_rm.ExecuteNonQuery();


                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Jwt (Token, UserId, DeviceId, CreatedAt, ExpiresAt)
                        VALUES (@token, @userId, @deviceId, NOW(), DATE_ADD(NOW(), INTERVAL @validitySeconds SECOND));
                        SELECT LAST_INSERT_ID();";

                    command.Parameters.AddWithValue("@token", jwtStr);
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@deviceId", deviceId);
                    command.Parameters.AddWithValue("@validitySeconds", validitySeconds);

                    var result = command.ExecuteScalar();
                    int jwtId;
                    if (result == null)
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME} Could not insert guest JWT into database: result is null");
                        throw new Exception("Could not insert guest JWT into database: result is null");
                    }
                    else
                    if (!int.TryParse(result.ToString(), out jwtId))
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME} Could not insert guest JWT into database: could not parse result to int");
                        throw new Exception("Could not insert guest JWT into database: could not parse result to int");
                    }
                }
            }
            else
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME} unknown UserType: {userType}");
                throw new Exception($"unknown UserType: {userType}");
            }

            return jwtStr;
        }

        public Gaos.Model.Token.TokenClaims? GetClaimsFormJWT(string jwt)
        {
            const string METHOD_NAME = "GetClaimsFormJWT()";
            try
            {
                if (publicKey == null)
                {
                    publicKey = RSAKeys.ReadPublicKey(GetPkcs12KeyStoreFilePath(), GetKeyStorePassword());

                }

                IDictionary<string, object> payload = Jose.JWT.Decode<IDictionary<string, object>>(jwt, publicKey, JwsAlgorithm.RS256);

                Gaos.Model.Token.TokenClaims claims = new Gaos.Model.Token.TokenClaims();
                claims.Sub = (string)payload["sub"]; 
                claims.Exp = (long)payload["exp"];

                long userIdLong = (long)payload["user_id"];
                int userIdInt;
                try { 
                    userIdInt = Convert.ToInt32(userIdLong);
                } 
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} cannot convert user id, long -> int");
                    throw new Exception("cannot convert user id, long -> int");
                }
                claims.UserId = userIdInt;

                string userType = (string)payload["user_type"];
                Gaos.Model.Token.UserType userTypeEnum;
                if (!Enum.TryParse(userType, out userTypeEnum)) {
                    Log.Warning($"{CLASS_NAME}:{METHOD_NAME} JWT is not valid, userType is not valid: {userType}");
                    return null;
                } else {
                    claims.UserType = userTypeEnum;
                }

                long deviceIdLong = (long)payload["device_id"];
                int deviceIdInt;
                try { 
                    deviceIdInt = Convert.ToInt32(deviceIdLong);
                } 
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} cannot convert device id, long -> int");
                    throw new Exception("cannot convert device id, long -> int");
                }
                claims.DeviceId = deviceIdInt;

                return claims;
            }
            catch (IntegrityException ex)
            {
                Log.Warning($"{CLASS_NAME}:{METHOD_NAME} JWT is not valid, IntegrityException: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME} An error occurred while decoding the JWT token: ");
                return null;
            }

        }

    }
}
