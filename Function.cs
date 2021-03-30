using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda_Notepad
{
    public class Function
    {

        public AmazonDynamoDBClient client = new AmazonDynamoDBClient(new AmazonDynamoDBConfig()
        {
            RegionEndpoint = RegionEndpoint.APSoutheast1
        });

        public static string tableName { get; } = "notepadDb";

        public async Task<PutItemResponse> CreateNotepad(CreateRequest request, ILambdaContext context)
        {
            var response = await client.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = tableName
            });

            Console.WriteLine(response);

            var createItemRequest = new PutItemRequest
            {
                TableName = tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { 
                        "Id",
                        new AttributeValue {N = $"{request.Id}"}
                    },
                    { 
                        "Title", 
                        new AttributeValue {S = $"{request.Title}"} 
                    },
                    { 
                        "Content", 
                        new AttributeValue {S = $"{request.Content}"} 
                    }
                }
            };

            var createItemResponse = await client.PutItemAsync(createItemRequest);
            return createItemResponse;
        }

        public async Task<Notepad> GetNotepad(GetRequest req, ILambdaContext context)
        {
            var request = new GetItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {N = $"{req.Id}"} }
                }
            };

            var response = await client.GetItemAsync(request);
            return ParseToNotepadObject(response.Item);
        }

        private Notepad ParseToNotepadObject(Dictionary<string, AttributeValue> attributeMap)
        {
            return new Notepad
            {
                Id = int.Parse(attributeMap.GetValueOrDefault("Id").N),
                Title = attributeMap.GetValueOrDefault("Title").S,
                Content = attributeMap.GetValueOrDefault("Content").S
            };
        }
    }

    public class CreateRequest : Notepad
    {
    }

    public class GetRequest
    {
        public int Id { get; set; }
    }

    public class Notepad
    {

        public int Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }
    }
}
