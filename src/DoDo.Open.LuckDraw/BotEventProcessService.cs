﻿using System.Text.RegularExpressions;
using DoDo.Open.Sdk.Models;
using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Events;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Services;

namespace DoDo.Open.LuckDraw
{
    public class BotEventProcessService : EventProcessService
    {
        private readonly OpenApiService _openApiService;
        private readonly OpenApiOptions _openApiOptions;
        private readonly AppSetting _appSetting;

        public BotEventProcessService(OpenApiService openApiService, AppSetting appSetting)
        {
            _openApiService = openApiService;
            _openApiOptions = openApiService.GetBotOptions();
            _appSetting = appSetting;
        }

        public override void Connected(string message)
        {
            _openApiOptions.Log?.Invoke($"Connected: {message}");

            #region 抽奖初始化

            if (!Directory.Exists($"{Environment.CurrentDirectory}\\data"))
            {
                Directory.CreateDirectory($"{Environment.CurrentDirectory}\\data");
            }
            if (!Directory.Exists($"{Environment.CurrentDirectory}\\data\\luck_draw"))
            {
                Directory.CreateDirectory($"{Environment.CurrentDirectory}\\data\\luck_draw");
            }
            if (!Directory.Exists($"{Environment.CurrentDirectory}\\data\\member"))
            {
                Directory.CreateDirectory($"{Environment.CurrentDirectory}\\data\\member");
            }

            #endregion
        }

        public override void Disconnected(string message)
        {
            _openApiOptions.Log?.Invoke($"Disconnected: {message}");
        }

        public override void Reconnected(string message)
        {
            _openApiOptions.Log?.Invoke($"Reconnected: {message}");
        }

        public override void Exception(string message)
        {
            _openApiOptions.Log?.Invoke($"Exception: {message}");
        }

        public override void Received(string message)
        {
            _openApiOptions.Log?.Invoke($"Received: {message}");
        }

        public override async void ChannelMessageEvent<T>(EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelMessage<T>>> input)
        {
            try
            {
                var eventBody = input.Data.EventBody;

                if (eventBody.MessageBody is MessageBodyText messageBodyText)
                {
                    var content = messageBodyText.Content.Replace(" ", "");
                    var defaultReply = $"<@!{eventBody.DodoId}>";
                    var reply = defaultReply;

                    #region 抽奖

                    var luckDrawDataPath = $"{Environment.CurrentDirectory}\\data\\luck_draw\\{eventBody.IslandId}.txt";
                    var memberDataPath = $"{Environment.CurrentDirectory}\\data\\member\\{eventBody.IslandId}.txt";

                    if (Regex.IsMatch(content, "^发起抽奖$"))
                    {
                        var cardEndTime = DateTime.Now.AddMinutes(1).GetTimeStamp();

                        var card = new Card
                        {
                            Type = "card",
                            Title = "发起抽奖",
                            Theme = "default",
                            Components = new List<object>()
                        };

                        card.Components.Add(new
                        {
                            type = "remark",
                            elements = new List<object>
                        {
                            new
                            {
                                type = "image",
                                src = eventBody.Personal.AvatarUrl
                            },
                            new
                            {
                                type = "dodo-md",
                                content = eventBody.Member.NickName
                            }
                        }
                        });

                        card.Components.Add(new
                        {
                            type = "section",
                            text = new
                            {
                                type = "dodo-md",
                                content = $"[{eventBody.DodoId}][{eventBody.Member.NickName}]发起的抽奖。"
                            }
                        });

                        card.Components.Add(new
                        {
                            type = "button-group",
                            elements = new List<object>
                        {
                            new
                            {
                                type = "button",
                                interactCustomId = "交互自定义id4",
                                click = new
                                {
                                    action = "form",
                                    value = ""
                                },
                                color = "grey",
                                name = "填写抽奖内容发起抽奖",
                                form = new
                                {
                                    title = "表单标题",
                                    elements = new List<object>
                                    {
                                        new
                                        {
                                            type = "input",
                                            key = "duration",
                                            title = "填写抽奖时间，填1时为1分钟。",
                                            rows = 1,
                                            placeholder = "请输入阿拉伯数字，1000字符限制",
                                            minChar = 0,
                                            maxChar = 1000
                                        },
                                        new
                                        {
                                            type = "input",
                                            key = "content",
                                            title = "填写抽奖的标题和内容",
                                            rows = 4,
                                            placeholder = "4000字符限制",
                                            minChar = 1,
                                            maxChar = 4000
                                        }
                                    }
                                }
                            }
                        }
                        });

                        card.Components.Add(new
                        {
                            type = "countdown",
                            title = "发起抽奖时，倒计时10分钟结束后失效",
                            style = "hour",
                            endTime = cardEndTime
                        });

                        var setChannelMessageSendOutput = await _openApiService.SetChannelMessageSendAsync(
                            new SetChannelMessageSendInput<MessageBodyCard>
                            {
                                ChannelId = eventBody.ChannelId,
                                MessageBody = new MessageBodyCard
                                {
                                    Card = card
                                }
                            }, true);

                        DataHelper.WriteValue(luckDrawDataPath, setChannelMessageSendOutput.MessageId, "Status", 1);
                        DataHelper.WriteValue(luckDrawDataPath, setChannelMessageSendOutput.MessageId, "EndTime", cardEndTime);
                        DataHelper.WriteValue(luckDrawDataPath, setChannelMessageSendOutput.MessageId, "Sponsor", eventBody.DodoId);
                        DataHelper.WriteValue(memberDataPath, eventBody.DodoId, "NickName", eventBody.Member.NickName);
                        DataHelper.WriteValue(memberDataPath, eventBody.DodoId, "AvatarUrl", eventBody.Personal.AvatarUrl);

                    }

                    #endregion

                    if (reply != defaultReply)
                    {
                        await _openApiService.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyText>
                        {
                            ChannelId = eventBody.ChannelId,
                            MessageBody = new MessageBodyText
                            {
                                Content = reply
                            }
                        });
                    }

                }
            }
            catch (Exception e)
            {
                Exception(e.Message);
            }
        }

        public override async void CardMessageButtonClickEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyCardMessageButtonClick>> input)
        {
            try
            {
                var eventBody = input.Data.EventBody;

                #region 抽奖

                var luckDrawDataPath = $"{Environment.CurrentDirectory}\\data\\luck_draw\\{eventBody.IslandId}.txt";
                var memberDataPath = $"{Environment.CurrentDirectory}\\data\\member\\{eventBody.IslandId}.txt";

                if (DataHelper.ReadValue<int>(luckDrawDataPath, eventBody.MessageId, "status") == 2)
                {
                    var cardEndTime = DataHelper.ReadValue<long>(luckDrawDataPath, eventBody.MessageId, "EndTime");
                    var cardContent = DataHelper.ReadValue<string>(luckDrawDataPath, eventBody.MessageId, "Content") ?? "";
                    var cardParticipants = DataHelper.ReadValue<string>(luckDrawDataPath, eventBody.MessageId, "Participants") ?? "";
                    var cardParticipantList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(cardParticipants))
                    {
                        cardParticipantList = cardParticipants.Split("|").ToList();
                    }

                    if (!cardParticipantList.Contains(eventBody.DodoId))
                    {
                        cardParticipantList.Add(eventBody.DodoId);
                        DataHelper.WriteValue(memberDataPath, eventBody.DodoId, "NickName", eventBody.Member.NickName);
                        DataHelper.WriteValue(memberDataPath, eventBody.DodoId, "AvatarUrl", eventBody.Personal.AvatarUrl);
                    }

                    var card = new Card
                    {
                        Type = "card",
                        Title = "抽奖",
                        Theme = "green",
                        Components = new List<object>()
                    };

                    card.Components.Add(new
                    {
                        type = "section",
                        text = new
                        {
                            type = "dodo-md",
                            content = cardContent
                        }
                    });

                    card.Components.Add(new
                    {
                        type = "divider"
                    });

                    var remarkElements = new List<object>();

                    foreach (var cardParticipant in cardParticipantList)
                    {
                        remarkElements.Add(new
                        {
                            type = "image",
                            src = DataHelper.ReadValue<string>(memberDataPath, cardParticipant, "AvatarUrl")
                        });
                        remarkElements.Add(new
                        {
                            type = "dodo-md",
                            content = DataHelper.ReadValue<string>(memberDataPath, cardParticipant, "NickName")
                        });
                    }

                    card.Components.Add(new
                    {
                        type = "remark",
                        elements = remarkElements
                    });

                    card.Components.Add(new
                    {
                        type = "divider"
                    });

                    card.Components.Add(new
                    {
                        type = "countdown",
                        title = "抽奖倒计时：",
                        style = "hour",
                        endTime = cardEndTime
                    });

                    card.Components.Add(new
                    {
                        type = "button-group",
                        elements = new List<object>
                        {
                            new
                            {
                                type = "button",
                                interactCustomId = "交互自定义id4",
                                click = new
                                {
                                    action = "call_back",
                                    value = "回传参数"
                                },
                                color = "green",
                                name = "每人只能点击一次，点击此处参与抽奖"
                            }
                        }
                    });

                    await _openApiService.SetChannelMessageEditAsync(new SetChannelMessageEditInput<MessageBodyCard>
                    {
                        MessageId = eventBody.MessageId,
                        MessageBody = new MessageBodyCard
                        {
                            Card = card
                        }
                    }, true);

                    DataHelper.WriteValue(luckDrawDataPath, eventBody.MessageId, "Participants", string.Join("|", cardParticipantList));
                }

                #endregion
            }
            catch (Exception e)
            {
                Exception(e.Message);
            }
        }

        public override async void CardMessageFormSubmitEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyCardMessageFormSubmit>> input)
        {
            try
            {
                var eventBody = input.Data.EventBody;

                #region 抽奖

                var luckDrawDataPath = $"{Environment.CurrentDirectory}\\data\\luck_draw\\{eventBody.IslandId}.txt";
                var memberDataPath = $"{Environment.CurrentDirectory}\\data\\member\\{eventBody.IslandId}.txt";

                if (DataHelper.ReadValue<int>(luckDrawDataPath, eventBody.MessageId, "status") == 1 && DataHelper.ReadValue<string>(luckDrawDataPath, eventBody.MessageId, "Sponsor") == eventBody.DodoId)
                {
                    var formDurationItem = eventBody.FormData.FirstOrDefault(x => x.Key == "duration")?.value ?? "";

                    if (!Regex.IsMatch(formDurationItem, @"\d+"))
                    {
                        formDurationItem = "10";
                    }

                    int.TryParse(formDurationItem, out var formDuration);

                    var cardEndTime = DateTime.Now.AddMinutes(formDuration).GetTimeStamp();
                    var cardContent = eventBody.FormData.FirstOrDefault(x => x.Key == "content")?.value ?? "";

                    var card = new Card
                    {
                        Type = "card",
                        Title = "抽奖",
                        Theme = "green",
                        Components = new List<object>()
                    };

                    card.Components.Add(new
                    {
                        type = "section",
                        text = new
                        {
                            type = "dodo-md",
                            content = cardContent
                        }
                    });

                    card.Components.Add(new
                    {
                        type = "divider"
                    });

                    card.Components.Add(new
                    {
                        type = "countdown",
                        title = "抽奖倒计时：",
                        style = "hour",
                        endTime = cardEndTime
                    });

                    card.Components.Add(new
                    {
                        type = "button-group",
                        elements = new List<object>
                        {
                            new
                            {
                                type = "button",
                                interactCustomId = "交互自定义id4",
                                click = new
                                {
                                    action = "call_back",
                                    value = "回传参数"
                                },
                                color = "green",
                                name = "每人只能点击一次，点击此处参与抽奖"
                            }
                        }
                    });

                    await _openApiService.SetChannelMessageEditAsync(new SetChannelMessageEditInput<MessageBodyCard>
                    {
                        MessageId = eventBody.MessageId,
                        MessageBody = new MessageBodyCard
                        {
                            Card = card
                        }
                    }, true);

                    DataHelper.WriteValue(luckDrawDataPath, eventBody.MessageId, "Status", 2);
                    DataHelper.WriteValue(luckDrawDataPath, eventBody.MessageId, "EndTime", cardEndTime);
                    DataHelper.WriteValue(luckDrawDataPath, eventBody.MessageId, "Content", cardContent.Replace("\n", "\\n"));
                }

                #endregion
            }
            catch (Exception e)
            {
                Exception(e.Message);
            }
        }
    }
}