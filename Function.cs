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

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string FunctionHandler(string input, ILambdaContext context)
        {
            return input?.ToUpper();
        }

        public async Task<PutItemResponse> CreateNotepad(Request request, ILambdaContext context)
        {
            var response = await client.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = "notepadDb"
            });

            Console.WriteLine(response);

            var createItemRequest = new PutItemRequest
            {
                TableName = "notepadDb",
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue {N = $"{request.Id}"} },
                    {"Title", new AttributeValue {S = $"{request.Title}"} },
                    {"Content", new AttributeValue {S = $"{request.Content}"} }
                }
            };

            var createItemResponse = await client.PutItemAsync(createItemRequest);
            return createItemResponse;
        }

        public async Task<Notepad> FetchNotepad(ILambdaContext context)
        {
            var request = new QueryRequest
            {
                TableName = "notepadDb",
                KeyConditionExpression = "Id = :v_Id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":v_Id", new AttributeValue {N = "1"} }
                }
            };

            var response = await client.QueryAsync(request);
            Dictionary<string, AttributeValue> dictionary = response.Items[0];
            return new Notepad
            {
                Id = Convert.ToInt32(dictionary.GetValueOrDefault("Id").N),
                Content = dictionary.GetValueOrDefault("Content").S,
                Title = dictionary.GetValueOrDefault("Title").S
            };
        }
    }

    public class Request : Notepad
    {
    }

    public class Notepad
    {

        public int Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }
    }
}
