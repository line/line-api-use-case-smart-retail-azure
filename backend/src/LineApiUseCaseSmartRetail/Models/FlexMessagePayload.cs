using System;
using System.Collections.Generic;

namespace LineApiUseCaseSmartRetail.Models
{
    /// <summary>
    /// LINE Messaging APIのFlexメッセージ作成用
    /// </summary>
    public class FlexMessagePayload
    {
        public string Type { get; set; }
        public string AltText { get; set; }
        public FlexMessageContents Contents { get; set; }

        public FlexMessagePayload() { }

        public FlexMessagePayload(float orderAmount, string detailsUrl)
        {
            Type = "flex";
            AltText = "この度は、Use Case Storeにてスマホレジをご利用いただきありがとうございました。";
            Contents = new FlexMessageContents
            {
                Type = "bubble",
                Header = new Header
                {
                    Type = "box",
                    Layout = "vertical",
                    Flex = 0,
                    Contents = new List<Content>
                    {
                        new Content
                        {
                            Type = "box",
                            Layout = "vertical",
                            Contents = new List<Content>
                            {
                                new Content
                                {
                                    Type = "text",
                                    Text = "Use Case Store ○×△ストア△□店",
                                    Weight = "bold",
                                    Size = "md",
                                    Contents = new List<Content>()
                                },
                                new Content
                                {
                                    Type = "text",
                                    Text = DateTime.UtcNow.ToJst().ToString("yyyy/MM/dd HH:mm:ss"),
                                    Color = "#767676",
                                    Contents = new List<Content>()
                                },
                            },
                        },
                    },
                },
                Body = new Body
                {
                    Type = "box",
                    Layout = "vertical",
                    Spacing = "lg",
                    Contents = new List<Content>
                    {
                        new Content
                        {
                            Type = "text",
                            Text = "この度は、Use Case Storeにてスマホレジをご利用いただきありがとうございました。\nご購入いただいた商品の明細は下記のリンクよりご確認いただけます。",
                            Size = "md",
                            Gravity = "center",
                            Wrap = true,
                            Contents = new List<Content>()
                        },
                        new Content
                        {
                            Type = "box",
                            Layout = "vertical",
                            Spacing = "sm",
                            Margin = "lg",
                            Contents = new List<Content>
                            {
                                new Content
                                {
                                    Type = "box",
                                    Layout = "baseline",
                                    Spacing = "sm",
                                    Contents = new List<Content>
                                    {
                                        new Content
                                        {
                                            Type = "text",
                                            Text = "税込合計金額",
                                            Size = "md",
                                            Color = "#AAAAAA",
                                            Flex = 2,
                                            Contents = new List<Content>()
                                        },
                                        new Content
                                        {
                                            Type = "text",
                                            Text = $"¥{orderAmount:#,0}",
                                            Size = "md",
                                            Color = "#666666",
                                            Flex = 2,
                                            Wrap = true,
                                            Contents = new List<Content>()
                                        },
                                    },
                                },
                            },
                        },
                        new Content
                        {
                            Type = "button",
                            Action = new Action
                            {
                                Type = "uri",
                                Label = "購入商品明細",
                                Uri = detailsUrl
                            },
                        },
                    },
                },
            };
        }

        public class FlexMessageContents
        {
            public string Type { get; set; }
            public string AltText { get; set; }
            public Header Header { get; set; }
            public Body Body { get; set; }
        }

        public class Header
        {
            public string Type { get; set; }
            public string Layout { get; set; }
            public int Flex { get; set; }
            public IEnumerable<Content> Contents { get; set; }
        }

        public class Body
        {
            public string Type { get; set; }
            public string Layout { get; set; }
            public string Spacing { get; set; }
            public IEnumerable<Content> Contents { get; set; }
        }

        public class Content
        {
            public string Type { get; set; }
            public string Layout { get; set; }
            public string Text { get; set; }
            public string Weight { get; set; }
            public string Size { get; set; }
            public string Color { get; set; }
            public string Gravity { get; set; }
            public bool? Wrap { get; set; }
            public string Spacing { get; set; }
            public string Margin { get; set; }
            public int Flex { get; set; }
            public Action Action { get; set; }
            public IEnumerable<Content> Contents { get; set; }
        }

        public class Action
        {
            public string Type { get; set; }
            public string Label { get; set; }
            public string Uri { get; set; }
        }
    }
}
