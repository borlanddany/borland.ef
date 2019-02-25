using System.Linq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Borland.EF.Tests
{
    public class EFLazyLoadingTests
    {
        [Test]
        public void DB_Has_3Posts()
        {
            ConsoleLoggerProvider.TextWriter = NUnit.Framework.TestContext.Out;
            NUnit.Framework.TestContext.Out.WriteLine("Init context");
            var context = new TestContext();
            var blog = new Blog { Name = "Test blog." };
            context.Blogs.Add(blog);
            NUnit.Framework.TestContext.Out.WriteLine("Add blog");
            context.SaveChanges();
            NUnit.Framework.TestContext.Out.WriteLine("Save changes");
            context.Posts.AddRange(new[]
            {
                new Post
                {
                    Title = "Post 1",
                    Content = "Content 1",
                    Blog = blog
                },
                new Post
                {
                    Title = "Post 2",
                    Content = "Content 2",
                    Blog = blog
                },
                new Post
                {
                    Title = "Post 3",
                    Content = "Content 3",
                    Blog = blog
                }
            });
            NUnit.Framework.TestContext.Out.WriteLine("Add posts");
            context.SaveChanges();
            NUnit.Framework.TestContext.Out.WriteLine("Save changes");
            var array = context.Posts.Include(x => x.Blog).Select(x => x.Blog).ToArray();
            Assert.IsTrue(array.All(x => x != null));
            Assert.AreEqual(3, array.Length);
        }
    }
}
