using MongoDB.Bson.Serialization.Attributes;

namespace GenericBot.Entities
{
    public class BlacklistedWord
    {
        [BsonId]
        public int Id { get; set; }
        public string Word { get; set; }
        public bool Active { get; set; }

        public BlacklistedWord()
        {
            Active = true;
        }

        public BlacklistedWord (string w, int i)
        {
            Word = w;
            Id = i;
            Active = true;
        }

        public override string ToString()
        {
            return $"\"{Word}\" (#{Id})";
        }
    }
}
