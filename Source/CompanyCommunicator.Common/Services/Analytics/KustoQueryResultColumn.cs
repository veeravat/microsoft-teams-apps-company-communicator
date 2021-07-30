namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Analytics
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class KustoQueryResultColumn
    {
        [JsonProperty("name")]
        public string ColumnName { get; set; }

        [JsonProperty("type")]
        public string ColumnType { get; set; }
    }

    public class KustoQueryResultTable
    {
        [JsonProperty("name")]
        public string TableName { get; set; }

        [JsonProperty("columns")]
        public List<KustoQueryResultColumn> Columns { get; set; }

        [JsonProperty("rows")]
        public List<List<object>> Rows { get; set; }
    }

    public class KustoQueryResult
    {
        [JsonProperty("tables")]
        public List<KustoQueryResultTable> Tables { get; set; }
    }
}
