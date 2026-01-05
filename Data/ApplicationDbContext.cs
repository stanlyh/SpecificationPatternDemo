using Microsoft.EntityFrameworkCore;

namespace SpecificationPatternDemo.Data;

public class ApplicationDbContext : DbContext
{
    internal IEnumerable<object> ApplySpecification(DotNetAndArchitecturePostSpecification specification)
    {
        throw new NotImplementedException();
    }
}
