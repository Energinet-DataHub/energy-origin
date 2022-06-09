namespace API.Models
{
    public class Result
    {
        public List<Record> Records { get; }

        public List<Field> Fields { get; }

        public string Sql { get; }

        public Result(List<Record> records, List<Field> fields, string sql)
        {
            Records = records;
            Fields = fields;
            Sql = sql;
        }
    }
}
