using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Blog.Models
{
    public class BlogPost
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string ImageURL { get; set; }
        public required string[] Tags { get; set; }
        public required string Content { get; set; }
    }
}
