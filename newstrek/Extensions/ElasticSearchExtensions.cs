using newstrek.Models;
using Nest;

namespace newstrek.Extensions
{
    public static class ElasticSearchExtensions
    {
        /*
            The this keyword is used to define an extension method for instances of IServiceCollection. This means that you are adding a new method called AddElasticSearch that can be called on an instance of IServiceCollection. 
        */
        public static void AddElasticSearch(this IServiceCollection services, IConfiguration configuration)
        {
            var url = configuration["ElasticSearchConfiguration:Uri"];
            var defaultIndex = configuration["ElasticSearchConfiguration:index"];
            // 之後ElasticSearch run在非本地的server時會需要credential

            var settings = new ConnectionSettings(new Uri(url))
                .PrettyJson()
                .DefaultIndex(defaultIndex);

            AddDefaultMappings(settings);

            var client = new ElasticClient(settings);
            services.AddSingleton<IElasticClient>(client);

            CreateIndex(client, defaultIndex);
        }

        private static void AddDefaultMappings(ConnectionSettings settings)
        {
            // Setting which field that need to be ignored by ElasticSearch
            settings.DefaultMappingFor<News>(n =>
                n.Ignore(x => x.Id));
        }

        private static void CreateIndex(IElasticClient client, string indexName)
        {
            //client.Indices.Create(indexName, i => i.Map<News>(x => x.AutoMap()));
            client.Indices.Create(indexName, i => i
                .Map<News>(m => m
                    .Properties(p => p
                        .Keyword(k => k
                            .Name(n => n.URL) // Field name "URL"
                        )
                        .Text(t => t
                            .Name(n => n.Date) // Field name "Date"
                        )
                        .Text(t => t
                            .Name(n => n.Title) // Field name "Title"
                        )
                        .Text(t => t
                            .Name(n => n.Article) // Field name "Article"
                        )
                        .Text(t => t
                            .Name(n => n.Category) // Field name "Category"
                        )
                        .Text(t => t
                            .Name(n => n.Tag) // Field name "Tag"
                        )
                    )
                )
            );
        }
    }
}
