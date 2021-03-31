using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda_Notepad
{
    public class Function
    {

        public static AmazonDynamoDBClient client = new AmazonDynamoDBClient(new AmazonDynamoDBConfig()
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

        public async Task<APIGatewayProxyResponse> GetNotepad(APIGatewayProxyRequest req, ILambdaContext context)
        {
            var request = new GetItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {N = $"{req.QueryStringParameters["Id"]}"} }
                }
            };

            var response = await client.GetItemAsync(request);
            return new APIGatewayProxyResponse { 
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonSerializer.Serialize(ParseToNotepadObject(response.Item)),
                Headers = new Dictionary<string, string> 
                { 
                    { "Content-Type", "application/json" },
                    { "Control-Access-Allow-Origin", "*" }
                }
            };
        }

        public async Task<List<Notepad>> GetAllNotepad(ILambdaContext context)
        {
            List<Notepad> listNotepad = new List<Notepad>();
            Table threadTable = Table.LoadTable(client, tableName);
            ScanFilter scanFilter = new ScanFilter();
            Search search = threadTable.Scan(scanFilter);
            var docs = await search.GetNextSetAsync();
            foreach (var doc in docs)
                listNotepad.Add(ExtractDocument(doc));
            return listNotepad;
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

        private static Notepad ExtractDocument(Document document)
        {
            Dictionary<string, string> notepadDic = new Dictionary<string, string>();
            foreach (var attribute in document.GetAttributeNames())
            {
                string stringValue = null;
                var value = document[attribute];
                if (value is Primitive)
                    stringValue = value.AsPrimitive().Value.ToString();
                else if (value is PrimitiveList)
                    stringValue = string.Join(",", (from primitive
                                    in value.AsPrimitiveList().Entries
                                                    select primitive.Value).ToArray());
                notepadDic.Add(attribute, stringValue);
            }

            return new Notepad
            {
                Id = int.Parse(notepadDic["Id"]),
                Title = notepadDic["Title"],
                Content = notepadDic["Content"]
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
