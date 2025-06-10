using MongoDB.Driver;
using Blog.Models;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;

namespace Blog
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add CORS policy for Vite frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowViteDevServer", policy =>
                {
                    policy.WithOrigins("http://localhost:5173",
                        "https://cephard.github.io/blog/")
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // MongoDB connection setup
            var config = app.Configuration;
            var mongoConnectionString = config.GetSection("BlogDatabaseSettings")["ConnectionString"];

            var client = new MongoClient(mongoConnectionString);
            var database = client.GetDatabase("blog");
            var blogCollection = database.GetCollection<BlogPost>("blogpost");

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

            // Enable Swagger in dev mode
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
