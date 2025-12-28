using MongoDB.Driver;
using Blog.Models;
using MongoDB.Bson;
using DotNetEnv;


namespace Blog
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            Env.Load();
            var builder = WebApplication.CreateBuilder(args);
            var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_DB_CONNECTION_STRING");
            var databaseName = Environment.GetEnvironmentVariable("DatabaseName");
            var collectionName = Environment.GetEnvironmentVariable("POSTS_COLLECTION_NAME");
            var localClient = Environment.GetEnvironmentVariable("LOCAL_HOST_CLIENT");
            var onlineClient = Environment.GetEnvironmentVariable("ONLINE_CLIENT");

            builder.Configuration.AddEnvironmentVariables();

            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add CORS policy for Vite frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowViteDevServer", policy =>
                {
                    policy.WithOrigins(onlineClient,localClient)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // MongoDB connection setup
            var config = app.Configuration;

            var client = new MongoClient(mongoConnectionString);
            var database = client.GetDatabase("blog");
            var blogCollection = database.GetCollection<BlogPost>("blogpost");

            app.MapGet("/", () => "This is for test -> Backend is running!");


            //end points
            //posting a new blogpost into mongodb
            app.MapPost("/blogposts", async (BlogPost newBlogPost) =>
            {
                try
                {
                    await blogCollection.InsertOneAsync(newBlogPost);
                    return Results.Created($"/blogposts/{newBlogPost.Id}", newBlogPost);
                }
                catch (Exception ex)
                {
                    return Results.Problem("Failed to post blog : " + ex.Message);

                }
            }
            ).WithName("CreateBlog")
             .WithTags("MongoDb");

            //updating the blogpost
            //replaces the entire parts of the blog can be expensive
            app.MapPut("/blogposts/{id}",async(string id, BlogPost blogToUpdate) => 
            {
                var modified = await blogCollection.ReplaceOneAsync(x => x.Id==id, blogToUpdate);
                return modified.ModifiedCount > 0 ? Results.Ok("Post Updated Suceesfully") : Results.NotFound();
            }
            ).WithName("UpdateBlog")
             .WithTags("MongoDb");

            //getting the blogposts
            // Getting the blogposts
            app.MapGet("/blogposts", async () => {
                var posts = await blogCollection.Find(_ => true).ToListAsync();
                return Results.Ok(posts);
            })
            .WithName("GetAllBlogPosts")
            .WithTags("MongoDb");

            //getting blogpost by title
            app.MapGet("/blogposts/{title}", async (string title) =>
            {
                var filter = Builders<BlogPost>.Filter.Regex("Title", new BsonRegularExpression($"^{title}", "i")); // "i" for case-insensitive
                var posts = await blogCollection.Find(filter).ToListAsync();
                return posts.Any() ? Results.Ok(posts) : Results.NotFound();
            })
            .WithName("BlogByTitle")
            .WithTags("MongoDb");


            //deleting the blogpost
            app.MapDelete("/blogposts/{id}", async (string id)=>
            {
                var postToDelete = await blogCollection.DeleteOneAsync(x => x.Id == id);
                return postToDelete.DeletedCount > 0 ? Results.Ok("Deleted!") : Results.NotFound();
            }).WithName("BlogToDelete")
            .WithTags("MongoDb");

            //Enable Swagger in dev mode
           if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
           }

            app.UseCors("AllowViteDevServer");
           // app.UseHttpsRedirection();
            app.UseAuthorization();

            app.Run();
            return Task.CompletedTask;
        }
    }
}
