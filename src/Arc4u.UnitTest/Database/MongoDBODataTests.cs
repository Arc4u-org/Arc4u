using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;

namespace Arc4u.UnitTest.Database;

/// <summary>
/// ASP.NET Core OData query options applied to the <see cref="IQueryable{T}"/> of a collection, as described in the MongoDB guide.
/// No server is needed: the queries are translated, not executed.
/// </summary>
[Trait("Category", "CI")]
public class MongoDBODataTests
{
    // The query options of the MongoDB guide: the ones the driver translates.
    private const AllowedQueryOptions SupportedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy | AllowedQueryOptions.Skip |
                                                              AllowedQueryOptions.Top | AllowedQueryOptions.Count;

    [Fact]
    public void Test_MongoDB_OData_Filter_OrderBy_Skip_Top_Should_Be_Translated()
    {
        var options = CreateQueryOptions("?$filter=Name eq 'Arc4u' and contains(Name,'rc')&$orderby=Name desc&$skip=10&$top=5");

        var query = options.ApplyTo(CreateFactory().GetCollection<Product>().AsQueryable());

        // The driver writes a translation error in ToString() instead of throwing it, so check the stages.
        var pipeline = query.ToString();

        pipeline.Should().StartWith("Shop.products.Aggregate(");
        pipeline.Should().Contain("{ \"$eq\" : [\"$Name\", \"Arc4u\"] }");
        pipeline.Should().Contain("{ \"$indexOfCP\" : [\"$Name\", \"rc\"] }");
        // OData adds the key to the sort to get a stable order.
        pipeline.Should().Contain("{ \"$sort\" : { \"Name\" : -1, \"_id\" : 1 } }");
        pipeline.Should().Contain("{ \"$skip\" : 10 }");
        pipeline.Should().Contain("{ \"$limit\" : 5 }");
    }

    [Fact]
    public void Test_MongoDB_OData_Select_Should_Not_Be_Translated()
    {
        var options = CreateQueryOptions("?$select=Name");

        var query = options.ApplyTo(CreateFactory().GetCollection<Product>().AsQueryable());

        // The driver cannot translate the projection OData builds for $select: it fails before reaching the server.
        // If a new driver version translates it (the server selection then times out instead), update the MongoDB guide.
        var exception = Record.Exception(() => query.GetEnumerator().MoveNext());

        exception.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [InlineData("?$filter=Name eq 'Arc4u'&$orderby=Name&$skip=10&$top=5&$count=true", true)]
    [InlineData("?$select=Name", false)]
    public void Test_MongoDB_OData_Supported_Query_Options_Should_Be_Validated(string queryString, bool isValid)
    {
        var options = CreateQueryOptions(queryString);

        var exception = Record.Exception(() => options.Validate(new ODataValidationSettings { AllowedQueryOptions = SupportedQueryOptions }));

        if (isValid)
        {
            exception.Should().BeNull();
        }
        else
        {
            exception.Should().BeOfType<ODataException>().Which.Message.Should().Contain("is not allowed");
        }
    }

    private static IMongoClientFactory<ShopDbContext> CreateFactory()
    {
        // A short server selection timeout, so that a query reaching the server fails fast: no test expects one to.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:mongo"] = "mongodb://localhost:27017/Shop?serverSelectionTimeoutMS=500" })
            .Build();

        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<ShopDbContext>(config, "mongo");

        return services.BuildServiceProvider().GetRequiredService<IMongoClientFactory<ShopDbContext>>();
    }

    private static ODataQueryOptions<Product> CreateQueryOptions(string queryString)
    {
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.EntitySet<Product>("Products");

        var context = new ODataQueryContext(modelBuilder.GetEdmModel(), typeof(Product), new ODataPath());

        // As AddOData(options => options.EnableQueryFeatures()) in the OData guide: every query option is enabled on every property.
        var configurations = context.DefaultQueryConfigurations;
        configurations.EnableFilter = true;
        configurations.EnableOrderBy = true;
        configurations.EnableSelect = true;
        configurations.EnableExpand = true;
        configurations.EnableCount = true;
        configurations.EnableSkipToken = true;
        configurations.MaxTop = null;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString(queryString);

        return new ODataQueryOptions<Product>(context, httpContext.Request);
    }

    public sealed class Product
    {
        [BsonId]
        public Guid Id { get; set; }

        public string Name { get; set; } = default!;
    }

    private sealed class ShopDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextBuilder context)
        {
            context.MapCollection("products").With<Product>();
        }
    }
}
