using System;
using System.Linq;

namespace SpecificationPatternDemo;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext db)
    {
        if (db.Posts.Any()) return;

        var posts = new[]
        {
            new Post
            {
                Title = ".NET and microservices",
                Content = "Discussion about microservices architecture in .NET",
                Category = ".NET",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Post
            {
                Title = "Architecture patterns",
                Content = "Exploring layered vs hexagonal architecture",
                Category = "Architecture",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Post
            {
                Title = "Other topic",
                Content = "Non related",
                Category = "Misc",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        db.Posts.AddRange(posts);
        db.SaveChanges();

        // Add some likes/comments for counts
        var first = db.Posts.First();
        db.Likes.AddRange(new[] { new Like { PostId = first.Id }, new Like { PostId = first.Id } });
        db.Comments.Add(new Comment { PostId = first.Id, Text = "Great post" });

        db.SaveChanges();
    }
}
