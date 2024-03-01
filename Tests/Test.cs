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

        public static void TestAll() {
            if (true) {
                TestJsonDiff();
            }
        }
    }
}
