#pragma warning disable 8600, 8601, 8618, 8602, 8604, 8603, 8765

using jsondiff;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace gaos.Tests
{
    public class Test
    {
        static void TestJsonDiff()
        {
            string jsonA = @"{ 'name': 'Alice', 'age': 30, 'city': 'New York', 'hobbies': ['reading', 'coding', 'dancing', 'flying'] }";
            string jsonB = @"{ 'name': 'Alice', 'age': 35, 'state': 'NY', 'hobbies': ['reading', 'coding', 'hiking'] }";

            JObject objA = JObject.Parse(jsonA);
            JObject objB = JObject.Parse(jsonB);

            var diff = Difference.CompareValues(objA, objB);

            var settings = Difference.GetJsonSerializerSettings();
            var diffJson = JsonConvert.SerializeObject(diff, settings);
            Console.WriteLine(diffJson);



            var diff2 = JsonConvert.DeserializeObject<DiffValue>(diffJson, settings);
            var diffJson2 = JsonConvert.SerializeObject(diff2, settings);
            Console.WriteLine(diffJson2);

            var objC = Difference.AddDiff(objA, diff);
            var objCJson = JsonConvert.SerializeObject(objC);
            Console.WriteLine(objCJson);

            Console.WriteLine("finished ok");

        }

        static void TestJsonDiff1()
        {
            string jsonA = @"{ 'name': 'Alice', 'age': 30, 'city': 'New York', 'hobbies': ['reading', 'coding', 'dancing', 'flying'] }";
            string jsonB = @"{ 'name': 'Alice', 'age': 35, 'state': 'NY', 'hobbies': [] }";

            JObject objA = JObject.Parse(jsonA);
            JObject objB = JObject.Parse(jsonB);

            var diff = Difference.CompareValues(objA, objB);

            var settings = Difference.GetJsonSerializerSettings();
            var diffJson = JsonConvert.SerializeObject(diff, settings);
            Console.WriteLine(diffJson);



            var diff2 = JsonConvert.DeserializeObject<DiffValue>(diffJson, settings);
            var diffJson2 = JsonConvert.SerializeObject(diff2, settings);
            Console.WriteLine(diffJson2);

            var objC = Difference.AddDiff(objA, diff);
            var objCJson = JsonConvert.SerializeObject(objC);
            Console.WriteLine(objCJson);

            Console.WriteLine("finished ok");

        }

        static void TestJsonIsEqual()
        {
            string jsonA = @"{ 'name': 'Alice', 'age': 30, 'city': 'New York', 'hobbies': ['reading', 'coding', 'dancing', 'flying'] }";
            string jsonB = @"{ 'age': 30, 'city': 'New York', 'name': 'Alice', 'hobbies': ['reading', 'flying', 'dancing', 'coding'] }";
            //string jsonB = @"{ 'name': 'Alice', 'age': 35, 'state': 'NY', 'hobbies': ['reading', 'coding', 'hiking'] }";

            JObject objA = JObject.Parse(jsonA);
            JObject objB = JObject.Parse(jsonB);

            var result = Difference.IsEqualValues(objA, objB);
            Console.WriteLine($"{result.IsEqual}, {result.PropertyPath}");

            Console.WriteLine("finished ok");

        }

        static void TestMergeBug()
        {
            // read json from file
            string jsonBase = File.ReadAllText("/w/pok/base.json");
            string jsonDiff = File.ReadAllText("/w/pok/diff.json");
            var settings = Difference.GetJsonSerializerSettings();

            var objBase = JObject.Parse(jsonBase);
            var objDiff = JObject.Parse(jsonDiff);
            var diff = JsonConvert.DeserializeObject<DiffValue>(jsonDiff, settings);

            var objResult = Difference.AddDiff(objBase, diff);
            var resultJson = JsonConvert.SerializeObject(objResult, settings);
            Console.WriteLine(resultJson);
        }

        static void TestEncryptionHelper()
        {
            string password = "password";
            string plaintext = "Hello, World!";

            string encrypted = Gaos.Encryption.EncryptionHelper.Encrypt(password, plaintext);
            Console.WriteLine($"Encrypted: {encrypted}");

            string decrypted = Gaos.Encryption.EncryptionHelper.Decrypt(password, encrypted);
            Console.WriteLine($"Decrypted: {decrypted}");
        }


        public static void TestAll() {
            if (true) {
                //TestJsonDiff();
                //TestJsonDiff1();
                //TestJsonIsEqual();
                //TestMergeBug();
                TestEncryptionHelper();
            }
        }
    }
}
