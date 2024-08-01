#pragma warning disable 8600, 8602, 8604

using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Serilog;
using Gaos.Auth;
using Gaos.Dbo;
using Gaos.Routes.Model.ChatRoomJson;
using Gaos.Dbo.Model;

namespace Gaos.Routes
{

    public static class ChatRoomRoutes
    {
        public static int MAX_NUMBER_OF_MESSAGES_IN_ROOM = 100;

        public static string CLASS_NAME = typeof(ChatRoomRoutes).Name;
        public static RouteGroupBuilder GroupChatRoom(this RouteGroupBuilder group)
        {
            group.MapGet("/hello", (Db db) => "hello");

            group.MapPost("/writeMessage", async (WriteMessageRequest writeMessageRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "chatRoom/writeMessage";
                try
                {
                    WriteMessageResponse response;

                    if (writeMessageRequest.Message == null || writeMessageRequest.Message.Trim() == "")
                    {
                        response = new WriteMessageResponse
                        {
                            IsError = true,
                            ErrorMessage = "parameter Message is empty",

                        };
                        return Results.Json(response);
                    }

                    // Verify that ChatRoomId exists
                    ChatRoom chatRoom = await db.ChatRoom.FirstOrDefaultAsync(x => x.Id == writeMessageRequest.ChatRoomId);

                    if (chatRoom == null)
                    {
                        response = new WriteMessageResponse
                        {
                            IsError = true,
                            ErrorMessage = "parameter ChatRoomId - no such chat room",

                        };
                        return Results.Json(response);

                    }

                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Read minimal and maximal message id
                            int minMessageId;
                            int maxMessageId;
                            int MessageCount = await db.ChatRoomMessage.CountAsync(x => x.ChatRoomId == writeMessageRequest.ChatRoomId);
                            if (MessageCount == 0)
                            {
                                minMessageId = 0;
                                maxMessageId = 0;
                            }
                            else
                            { 
                                minMessageId = await db.ChatRoomMessage.Where(x => x.ChatRoomId == writeMessageRequest.ChatRoomId).MinAsync(x => x.MessageId);
                                maxMessageId = await db.ChatRoomMessage.Where(x => x.ChatRoomId == writeMessageRequest.ChatRoomId).MaxAsync(x => x.MessageId);
                            }

                            // Remove messages if there are more than MAX_NUMBER_OF_MESSAGES_IN_ROOM 
                            if (maxMessageId - minMessageId > MAX_NUMBER_OF_MESSAGES_IN_ROOM)
                            {
                                await db.Database.ExecuteSqlRawAsync("DELETE FROM ChatRoomMessage WHERE ChatRoomId = {0} AND MesageId < {1}", writeMessageRequest.ChatRoomId, maxMessageId - MAX_NUMBER_OF_MESSAGES_IN_ROOM);
                            }

                            // Verify if user is a member of the chat room
                            ChatRoomMember chatRoomMember = await db.ChatRoomMember.FirstOrDefaultAsync(x => x.ChatRoomId == writeMessageRequest.ChatRoomId && x.UserId == userService.GetUserId());
                            if (chatRoomMember == null)
                            {
                                transaction.Rollback();
                                response = new WriteMessageResponse
                                {
                                    IsError = true,
                                    ErrorMessage = "user is not a member of chat room",

                                };
                                return Results.Json(response);

                            }

                            Gaos.Dbo.Model.User user = userService.GetUser();

                            // Create new message
                            ChatRoomMessage chatRoomMessage = new ChatRoomMessage
                            {
                                ChatRoomId = writeMessageRequest.ChatRoomId,
                                UserId = user.Id,
                                ChatRoomMemberName = user.Name,
                                MessageId = (int)(maxMessageId + 1),
                                Message = writeMessageRequest.Message,
                                CreatedAt = System.DateTime.Now,
                            };

                            // Save message
                            await db.ChatRoomMessage.AddAsync(chatRoomMessage);
                            await db.SaveChangesAsync();

                            // Commit transaction
                            transaction.Commit();

                            // Return response
                            response = new WriteMessageResponse
                            {
                                IsError = false,
                                MinMessageId = minMessageId,
                                MaxMessageId = maxMessageId,
                                MessageCount = MessageCount,
                            };
                            return Results.Json(response);


                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                            response = new WriteMessageResponse
                            {
                                IsError = true,
                                ErrorMessage = "internal error",
                            };
                            return Results.Json(response);
                        }
                    }
                }
                catch (Exception ex) 
                { 
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    WriteMessageResponse response = new WriteMessageResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error 1",
                    };
                    return Results.Json(response);
                }
                
            });

            group.MapPost("/readMessages", async (ReadMessagesRequest readMessagesRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "chatRoom/readMessages";
                ReadMessagesResponse response;
                try
                {
                    // Verify that ChatRoomId exists
                    ChatRoom chatRoom = await db.ChatRoom.FirstOrDefaultAsync(x => x.Id == readMessagesRequest.ChatRoomId);

                    if (chatRoom == null)
                    {
                        response = new ReadMessagesResponse
                        {
                            IsError = true,
                            ErrorMessage = "ChatRoomId - no such chat room",

                        };
                        return Results.Json(response);

                    }

                    // Verify if user is a member of the chat room
                    ChatRoomMember chatRoomMember = await db.ChatRoomMember.FirstOrDefaultAsync(x => x.ChatRoomId == readMessagesRequest.ChatRoomId && x.UserId == userService.GetUserId());
                    if (chatRoomMember == null)
                    {
                        response = new ReadMessagesResponse
                        {
                            IsError = true,
                            ErrorMessage = "user is not a member of chat room",
                        };
                        return Results.Json(response);
                    }

                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Read minimal and maximal message id
                            int minMessageId;
                            int maxMessageId;
                            int messageCount = await db.ChatRoomMessage.CountAsync(x => x.ChatRoomId == readMessagesRequest.ChatRoomId);
                            if (messageCount == 0)
                            {
                                minMessageId = 0;
                                maxMessageId = 0;
                            }
                            else
                            { 
                                minMessageId = await db.ChatRoomMessage.Where(x => x.ChatRoomId == readMessagesRequest.ChatRoomId).MinAsync(x => x.MessageId);
                                maxMessageId = await db.ChatRoomMessage.Where(x => x.ChatRoomId == readMessagesRequest.ChatRoomId).MaxAsync(x => x.MessageId);
                            }

                            // Read messages
                            ChatRoomMessage[] chatRoomMessages = await db.ChatRoomMessage.Where(x => x.ChatRoomId == readMessagesRequest.ChatRoomId && x.MessageId > readMessagesRequest.LastMessageId).OrderBy(x => x.MessageId).Take(readMessagesRequest.Count).ToArrayAsync();

                            ResponseMessage[] messages = new ResponseMessage[chatRoomMessages.Length];
                            for (int i = 0; i < chatRoomMessages.Length; i++)
                            {
                                messages[i] = new ResponseMessage
                                {
                                    MessageId = chatRoomMessages[i].MessageId,
                                    Message = chatRoomMessages[i].Message,
                                    CreatedAt = chatRoomMessages[i].CreatedAt,
                                    UserId = chatRoomMessages[i].UserId,
                                    UserName = chatRoomMessages[i].ChatRoomMemberName,
                                };
                            }

                            // Return response
                            response = new ReadMessagesResponse
                            {
                                IsError = false,
                                Messages = messages,
                                MinMessageId = minMessageId,
                                MaxMessageId = maxMessageId,
                                MessageCount = messageCount,
                            };
                            return Results.Json(response);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                            response = new ReadMessagesResponse
                            {
                                IsError = true,
                                ErrorMessage = "internal error",
                            };
                            return Results.Json(response);
                        }
                    }


                    
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    response = new ReadMessagesResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/readMessagesBackwards", async (ReadMessagesBackwardsRequest readMessagesBackwardsRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "chatRoom/readMessagesBackwards";
                ReadMessagesBackwardsResponse response;
                try
                {
                    // Verify that ChatRoomId exists
                    ChatRoom chatRoom = await db.ChatRoom.FirstOrDefaultAsync(x => x.Id == readMessagesBackwardsRequest.ChatRoomId);

                    if (chatRoom == null)
                    {
                        response = new ReadMessagesBackwardsResponse
                        {
                            IsError = true,
                            ErrorMessage = "ChatRoomId - no such chat room",

                        };
                        return Results.Json(response);

                    }

                    // Verify if user is a member of the chat room
                    ChatRoomMember chatRoomMember = await db.ChatRoomMember.FirstOrDefaultAsync(x => x.ChatRoomId == readMessagesBackwardsRequest.ChatRoomId && x.UserId == userService.GetUserId());
                    if (chatRoomMember == null)
                    {
                        response = new ReadMessagesBackwardsResponse
                        {
                            IsError = true,
                            ErrorMessage = "user is not a member of chat room",
                        };
                        return Results.Json(response);
                    }

                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        { 
                            // Read minimal and maximal message id
                            int minMessageId;
                            int maxMessageId;
                            int messageCount = await db.ChatRoomMessage.CountAsync(x => x.ChatRoomId == readMessagesBackwardsRequest.ChatRoomId);
                            if (messageCount == 0)
                            {
                                minMessageId = 0;
                                maxMessageId = 0;
                            }
                            else
                            { 
                                minMessageId = await db.ChatRoomMessage.Where(x => x.ChatRoomId == readMessagesBackwardsRequest.ChatRoomId).MinAsync(x => x.MessageId);
                                maxMessageId = await db.ChatRoomMessage.Where(x => x.ChatRoomId == readMessagesBackwardsRequest.ChatRoomId).MaxAsync(x => x.MessageId);
                            }

                            int lastMessageId;
                            if (readMessagesBackwardsRequest.LastMessageId <= 0)
                            {
                                lastMessageId = Int32.MaxValue;
                            }
                            else
                            {
                                lastMessageId = readMessagesBackwardsRequest.LastMessageId;
                            }

                            // Read messages
                            ChatRoomMessage[] chatRoomMessages = await db.ChatRoomMessage.Where(x => x.ChatRoomId == readMessagesBackwardsRequest.ChatRoomId && x.MessageId < lastMessageId).OrderBy(x => x.MessageId).Take(readMessagesBackwardsRequest.Count).ToArrayAsync();

                            ResponseMessage[] messages = new ResponseMessage[chatRoomMessages.Length];
                            for (int i = 0; i < chatRoomMessages.Length; i++)
                            {
                                messages[i] = new ResponseMessage
                                {
                                    MessageId = chatRoomMessages[i].MessageId,
                                    Message = chatRoomMessages[i].Message,
                                    CreatedAt = chatRoomMessages[i].CreatedAt,
                                    UserId = chatRoomMessages[i].UserId,
                                    UserName = chatRoomMessages[i].ChatRoomMemberName,
                                };
                            }

                            // Return response
                            response = new ReadMessagesBackwardsResponse
                            {
                                IsError = false,
                                Messages = messages,
                                MinMessageId = minMessageId,
                                MaxMessageId = maxMessageId,
                                MessageCount = messageCount,
                            };
                            return Results.Json(response);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                            response = new ReadMessagesBackwardsResponse
                            {
                                IsError = true,
                                ErrorMessage = "internal error",
                            };
                            return Results.Json(response);
                        }
                    }


                    
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    response = new ReadMessagesBackwardsResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/existsChatRoom", async (ExistsChatRoomRequest existsChatRoomRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "chatRoom/existsChatRoom";
                ExistsChatRoomResponse response;
                try
                {
                    if (existsChatRoomRequest.ChatRoomName == null || existsChatRoomRequest.ChatRoomName == "")
                    {
                        response = new ExistsChatRoomResponse
                        {
                            IsError = true,
                            ErrorMessage = "parameter ChatRoomName is empty",
                        };
                        return Results.Json(response);
                    }

                    // Verify that ChatRoomName exists 
                    var chatRoomData = await db.ChatRoom.Where(x => x.Name == existsChatRoomRequest.ChatRoomName).Select(x => new { x.Id, x.Name }).FirstOrDefaultAsync();

                    if (chatRoomData == null)
                    {
                        response = new ExistsChatRoomResponse
                        {
                            IsError = false,
                            IsExists = false,
                            ChatRoomId = -1,
                        };
                        return Results.Json(response);
                    }
                    else
                    {
                        response = new ExistsChatRoomResponse
                        {
                            IsError = false,
                            IsExists = true,
                            ChatRoomId = chatRoomData.Id,
                        };
                        return Results.Json(response);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    response = new ExistsChatRoomResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }

            });

            group.MapPost("/createChatRoom", async (CreateChatRoomRequest createChatRoomRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "chatRoom/createChatRoom";
                CreateChatRoomResponse response;
                try
                {
                    if (createChatRoomRequest.ChatRoomName == null || createChatRoomRequest.ChatRoomName == "")
                    {
                        response = new CreateChatRoomResponse
                        {
                            IsError = true,
                            ErrorMessage = "parameter ChatRoomName is empty",
                        };
                        return Results.Json(response);
                    }

                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            Gaos.Dbo.Model.ChatRoom chatRoom = new Gaos.Dbo.Model.ChatRoom
                            {
                                Name = createChatRoomRequest.ChatRoomName,
                                OwnerId = userService.GetUserId(),
                            };

                            // Save chat room
                            db.ChatRoom.Add(chatRoom);
                            await db.SaveChangesAsync();

                            // Add owner as a member
                            ChatRoomMember chatRoomMember = new ChatRoomMember
                            {
                                ChatRoomId = chatRoom.Id,
                                UserId = userService.GetUserId(),
                            };
                            db.ChatRoomMember.Add(chatRoomMember);

                            await db.SaveChangesAsync();

                            await transaction.CommitAsync();

                            // Return response
                            response = new CreateChatRoomResponse
                            {
                                IsError = false,
                                ChatRoomId = chatRoom.Id,
                            };
                            return Results.Json(response);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                            response = new CreateChatRoomResponse
                            {
                                IsError = true,
                                ErrorMessage = "internal error",
                            };
                            return Results.Json(response);
                        }
                    }



                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    response = new CreateChatRoomResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/deleteChatRoom", async (DeleteChatRoomRequest deleteChatRoomRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "chatRoom/deleteChatRoom";
                DeleteChatRoomResponse response;
                try
                {
                    // Remove all messages from the chat room using raw SQL
                    await db.Database.ExecuteSqlAsync($"DELETE FROM ChatRoomMessage WHERE ChatRoomId = {deleteChatRoomRequest.ChatRoomId}");
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Remove all messages from the chat room using raw SQL
                            int v = await db.Database.ExecuteSqlAsync($"DELETE FROM ChatRoomMessage WHERE ChatRoomId = {deleteChatRoomRequest.ChatRoomId}");

                            // Remove all members from the chat room using raw SQL
                            await db.Database.ExecuteSqlAsync($"DELETE FROM ChatRoomMember WHERE ChatRoomId = {deleteChatRoomRequest.ChatRoomId}");

                            // Remove chat room using raw SQL
                            await db.Database.ExecuteSqlAsync($"DELETE FROM ChatRoom WHERE Id = {deleteChatRoomRequest.ChatRoomId}");

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                            response = new DeleteChatRoomResponse
                            {
                                IsError = true,
                                ErrorMessage = "internal error",
                            };
                            return Results.Json(response);
                        }
                    }

                    // Return response
                    response = new DeleteChatRoomResponse
                    {
                        IsError = false,
                    };
                    return Results.Json(response);
                    

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    response = new DeleteChatRoomResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/addMember", async (AddMemberRequest addMemberRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "chatRoom/addMember";
                AddMemberResponse response;
                try
                {
                    // Verify chat room exists
                    Gaos.Dbo.Model.ChatRoom chatRoom = await db.ChatRoom.FirstOrDefaultAsync(x => x.Id == addMemberRequest.ChatRoomId);
                    if (chatRoom == null)
                    {
                        response = new AddMemberResponse
                        {
                            IsError = true,
                            ErrorMessage = "chat room does not exist",
                        };
                        return Results.Json(response);
                    }

                    // Get all members of the chat room
                    int[] chatRoomMembers = await db.ChatRoomMember.Where(x => x.ChatRoomId == addMemberRequest.ChatRoomId).Select(x => x.UserId).ToArrayAsync();

                    bool loogedInUserIsMember = chatRoomMembers.Contains(userService.GetUserId());
                    bool loggedInUserIsOwner = chatRoom.OwnerId == userService.GetUserId();

                    if (!loggedInUserIsOwner)
                    {
                        response = new AddMemberResponse
                        {
                            IsError = true,
                            ErrorMessage = "logged in user is not an owner of chatroom, only the owner may add new members ",
                        };
                        return Results.Json(response);
                    }

                    bool userIsAlreadyMember = chatRoomMembers.Contains(addMemberRequest.UserId);
                    if (userIsAlreadyMember)
                    {
                        Log.Warning($"{CLASS_NAME}:{METHOD_NAME}: user is already a member of the chat room");
                        response = new AddMemberResponse
                        {
                            IsError = false,
                        };
                        return Results.Json(response);
                    }
                    

                    // Add member to chat room
                    ChatRoomMember chatRoomMember = new ChatRoomMember
                    {
                        ChatRoomId = addMemberRequest.ChatRoomId,
                        UserId = addMemberRequest.UserId,
                    };
                    db.ChatRoomMember.Add(chatRoomMember);
                    await db.SaveChangesAsync();

                    // Return response
                    response = new AddMemberResponse
                    {
                        IsError = false,
                    };
                    return Results.Json(response);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    response = new AddMemberResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });

            group.MapPost("/removeMember", async (RemoveMemberRequest removeMemberRequest, Db db, Gaos.Common.UserService userService) =>
            {
                const string METHOD_NAME = "chatRoom/removeMember";
                AddMemberResponse response;
                try
                {
                    // Verify chat room exists
                    Gaos.Dbo.Model.ChatRoom chatRoom = await db.ChatRoom.FirstOrDefaultAsync(x => x.Id == removeMemberRequest.ChatRoomId);
                    if (chatRoom == null)
                    {
                        response = new AddMemberResponse
                        {
                            IsError = true,
                            ErrorMessage = "chat room does not exist",
                        };
                        return Results.Json(response);
                    }

                    ChatRoomMember chatRoomMember = await db.ChatRoomMember.FirstOrDefaultAsync(x => x.ChatRoomId == removeMemberRequest.ChatRoomId && x.UserId == removeMemberRequest.UserId);
                    if (chatRoomMember == null)
                    {
                        response = new AddMemberResponse
                        {
                            IsError = false,
                        };
                        return Results.Json(response);
                    }

                    int userId = userService.GetUserId();

                    if (chatRoom.OwnerId != userId && chatRoomMember.UserId != userId)
                    {
                        response = new AddMemberResponse
                        {
                            IsError = true,
                            ErrorMessage = "unauthorized (chat room owner can removen any member, member can remove itself)",
                        };
                        return Results.Json(response);
                    }

                    // Remove member from chat room
                    db.ChatRoomMember.Remove(chatRoomMember);
                    await db.SaveChangesAsync();

                    // Return response
                    response = new AddMemberResponse
                    {
                        IsError = false,
                    };
                    return Results.Json(response);


                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error: {ex.Message}");
                    response = new AddMemberResponse
                    {
                        IsError = true,
                        ErrorMessage = "internal error",
                    };
                    return Results.Json(response);
                }
            });



            return group;

        }
    }
}
