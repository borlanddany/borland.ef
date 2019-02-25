using System.Linq;

namespace Borland.EF.Tests
{
    public class Blog
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual IQueryable/*IEnumerable*/<Post> Posts { get; set; }
    }
}
