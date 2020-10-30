using Digimezzo.Foundation.Core.IO;
using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Alex; //Digimezzo.Foundation.Core.Settings
using Dopamine.Services.Provider;
using NLog.Layouts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Dopamine.Services.Provider
{

    public class ProviderService : IProviderService
    {
        private string providersXmlPath;
        private XDocument providersDocument;
    
        public ProviderService()
        {
            this.providersXmlPath = Path.Combine(SettingsClient.ApplicationFolder(), "Providers.xml");

            // Create the XML containing the Providers
            this.CreateProvidersXml();

            // Load the XML containing the Providers
            this.LoadProvidersXml();
        }
    
        public event EventHandler SearchProvidersChanged = delegate { };
      
        private void CreateProvidersXml()
        {
            // Only create this file if it doesn't yet exist. That allows the user to provide 
            // custom providers, without overwriting them the next time the application loads.
            if (!System.IO.File.Exists(this.providersXmlPath))
            {
                XDocument providersDocument = XDocument.Parse(
               @"<?xml version=""1.0"" encoding=""utf-8""?>
<Providers>
    <SearchProviders>
        <SearchProvider>
            <Id>e76c9f60-ef0e-4468-b47a-2889810fde85</Id>
            <Name>Video (YouTube)</Name>
            <Url>https://www.youtube.com/results?search_query=</Url>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>0d08bb4d-68b1-4c19-b952-e76d06d198fa</Id>
            <Name>Lyrics (Musixmatch)</Name>
            <Url>https://www.musixmatch.com/search/</Url>
            <Separator>%20</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>6eec2184-6548-4e89-95a0-6462be33689d</Id>
            <Name>Google (Lyrics)</Name>
            <Url>https://www.google.com/search?q=lyrics+</Url>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>5f3568ee-92af-4c82-a35f-377383e5d163</Id>
            <Name>AllMusic</Name>
            <Url>https://www.allmusic.com/search/songs/</Url>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>14190168-57aa-4049-b3a5-205f6140078d</Id>
            <Name>Google</Name>
            <Url>https://www.google.com/search?q=</Url>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>e79ecd51-e36a-40e1-921e-3d3b8cca0904</Id>
            <Name>Song Meanings</Name>
            <Url>https://songmeanings.com/query/?type=songtitles&amp;query=</Url>
            <Separator>%20</Separator>
        </SearchProvider>


<!-- Artists -->
        <SearchProvider>
            <Id>c8d2231b-9fc6-474f-8acb-27600755efcc</Id>
            <Name>Video (YouTube)</Name>
            <Url>https://www.youtube.com/results?search_query=</Url>
            <Type>Artist</Type>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>d5b02dfb-b125-48e3-8687-ea802c3917ba</Id>
            <Name>AllMusic</Name>
            <Url>https://www.allmusic.com/search/artists/</Url>
            <Type>Artist</Type>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>13ddc037-0deb-4f3e-b79b-c905eab03e56</Id>
            <Name>Google</Name>
            <Url>https://www.google.com/search?q=</Url>
            <Type>Artist</Type>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>bf3ee7a6-7dbb-4f7f-9b68-6adb9e188599</Id>
            <Name>Google (Images)</Name>
            <Url>https://www.google.com/search?tbm=isch&amp;q=</Url>
            <Type>Artist</Type>
            <Separator>+</Separator>
        </SearchProvider>

<!-- Albums -->
        <SearchProvider>
            <Id>1fccb983-c18d-4c53-9fd3-7b5a7099e678</Id>
            <Name>Video (YouTube)</Name>
            <Url>https://www.youtube.com/results?search_query=</Url>
            <Type>Album</Type>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>0952d214-8e4f-4c40-8405-8aae63889221</Id>
            <Name>AllMusic</Name>
            <Url>https://www.allmusic.com/search/albums/</Url>
            <Type>Album</Type>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>2d770396-b88a-4668-808d-05792e6dd651</Id>
            <Name>Google</Name>
            <Url>https://www.google.com/search?q=</Url>
            <Type>Album</Type>
            <Separator>+</Separator>
        </SearchProvider>
        <SearchProvider>
            <Id>a3fed47e-74fb-41a2-bda0-8dae94c06e4c</Id>
            <Name>Google (Images)</Name>
            <Url>https://www.google.com/search?tbm=isch&amp;q=</Url>
            <Type>Album</Type>
            <Separator>+</Separator>
        </SearchProvider>


    </SearchProviders>
</Providers>");

                providersDocument.Save(this.providersXmlPath);
            }
        }

        private void LoadProvidersXml()
        {
            if (this.providersDocument == null)
            {
                try
                {
                    providersDocument = XDocument.Load(this.providersXmlPath);
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not load providers XML. Exception: {0}", ex.Message);
                }

            }
        }

        public async Task<List<SearchProvider>> GetSearchProvidersAsync(SearchProvider.ProviderType providerType)
        {
            var providers = new List<SearchProvider>();

            await Task.Run(() =>
            {
                try
                {
                    if (this.providersDocument != null)
                    {
                        providers = (from t in this.providersDocument.Element("Providers").Elements("SearchProviders")
                                     from p in t.Elements("SearchProvider")
                                     from i in p.Elements("Id")
                                     from n in p.Elements("Name")
                                     from u in p.Elements("Url")
                                     from s in p.Elements("Separator")
                                     select new SearchProvider
                                     {
                                         Id = i.Value,
                                         Name = n.Value,
                                         Url = u.Value,
                                         Separator = s.Value,
                                         Type = StringToProviderType(p.Elements("Type")?.FirstOrDefault()?.Value)
                                     }).Distinct().Where(x => x.Type == providerType).ToList();
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not load search providers. Exception: {0}", ex.Message);
                }

            });

            return providers.OrderBy((p) => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private SearchProvider.ProviderType StringToProviderType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return SearchProvider.ProviderType.Track;
            type = type.ToLower();
            if (type.Equals(SearchProvider.ProviderType.Artist.ToString().ToLower()))
                return SearchProvider.ProviderType.Artist;
            if (type.Equals(SearchProvider.ProviderType.Album.ToString().ToLower()))
                return SearchProvider.ProviderType.Album;
            if (type.Equals(SearchProvider.ProviderType.Track.ToString().ToLower()))
                return SearchProvider.ProviderType.Track;
            Debug.Assert(false, $"StringToProviderType {type} is unkonwn");
            return SearchProvider.ProviderType.Track;
        }

        public void SearchOnline(string id, string[] searchArguments)
        {
            string url = string.Empty;

            try
            {
                var provider = (from t in this.providersDocument.Element("Providers").Elements("SearchProviders")
                                from p in t.Elements("SearchProvider")
                                from i in p.Elements("Id")
                                from n in p.Elements("Name")
                                from u in p.Elements("Url")
                                from s in p.Elements("Separator")
                                where i.Value == id
                                select new SearchProvider
                                {
                                    Id = i.Value,
                                    Name = n.Value,
                                    Url = u.Value,
                                    Separator = !string.IsNullOrWhiteSpace(s.Value) ? s.Value : "%20"
                                }).FirstOrDefault();

                url = provider.Url + string.Join(provider.Separator, searchArguments).Replace("&", provider.Separator); // Recplace "&" because Youtube forgets the part of he URL that comes after "&"

                Actions.TryOpenLink(url); 
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not search online using url: '{0}'. Exception: {1}", url, ex.Message);
            }
        }

        public bool RemoveSearchProvider(SearchProvider provider)
        {
            bool returnValue = false;

            XElement providerElementToRemove = (from t in this.providersDocument.Element("Providers").Elements("SearchProviders")
                                                from p in t.Elements("SearchProvider")
                                                from i in p.Elements("Id")
                                                where i.Value == provider.Id
                                                select p).FirstOrDefault();

            if (providerElementToRemove != null)
            {
                try
                {
                    providerElementToRemove.Remove();
                    this.providersDocument.Save(this.providersXmlPath);
                    returnValue = true;
                    this.SearchProvidersChanged(this, new EventArgs());
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not remove search provider. Exception: {0}", ex.Message);
                }
            }

            return returnValue;
        }

        public UpdateSearchProviderResult AddSearchProvider(SearchProvider provider)
        {
            if (string.IsNullOrEmpty(provider.Name) | string.IsNullOrEmpty(provider.Url))
            {
                LogClient.Error("The online search provider could not be added. Fields 'Name' and 'Url' are required, 'Separator' is optional.");
                return UpdateSearchProviderResult.MissingFields;
            }

            try
            {
                XElement searchProvider = new XElement("SearchProvider");
                searchProvider.SetElementValue("Id", Guid.NewGuid().ToString());
                searchProvider.SetElementValue("Name", provider.Name);
                searchProvider.SetElementValue("Url", provider.Url);
                searchProvider.SetElementValue("Separator", provider.Separator != null ? provider.Separator : string.Empty);

                this.providersDocument.Element("Providers").Element("SearchProviders").Add(searchProvider);

                this.providersDocument.Save(this.providersXmlPath);
                this.SearchProvidersChanged(this, new EventArgs());

                return UpdateSearchProviderResult.Success;
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not update search provider. Exception: {0}", ex.Message);
            }

            return UpdateSearchProviderResult.Failure;
        }

        public UpdateSearchProviderResult UpdateSearchProvider(SearchProvider provider)
        {
            if (string.IsNullOrEmpty(provider.Name) | string.IsNullOrEmpty(provider.Url))
            {
                LogClient.Error("The online search provider could not be updated. Fields 'Name' and 'Url' are required, 'Separator' is optional.");
                return UpdateSearchProviderResult.MissingFields;
            }

            try
            {
                XElement providerElementToUpdate = (from t in this.providersDocument.Element("Providers").Elements("SearchProviders")
                                                    from p in t.Elements("SearchProvider")
                                                    from i in p.Elements("Id")
                                                    where i.Value == provider.Id
                                                    select p).FirstOrDefault();

                if (providerElementToUpdate == null) return UpdateSearchProviderResult.Failure;

                providerElementToUpdate.SetElementValue("Name", provider.Name);
                providerElementToUpdate.SetElementValue("Url", provider.Url);
                providerElementToUpdate.SetElementValue("Separator", provider.Separator);

                this.providersDocument.Save(this.providersXmlPath);
                this.SearchProvidersChanged(this, new EventArgs());

                return UpdateSearchProviderResult.Success;
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not update search provider. Exception: {0}", ex.Message);
            }

            return UpdateSearchProviderResult.Failure;
        }
    }
}