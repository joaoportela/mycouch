﻿using System;
using System.Linq;
using MyCouch.Requests;
using MyCouch.Testing.Model;
using MyCouch.Testing.TestData;

namespace MyCouch.IntegrationTests.TestFixtures
{
    public class ViewsFixture : IDisposable
    {
        protected IClient Client;

        public Artist[] Artists { get; protected set; }

        public ViewsFixture()
        {
            Artists = ClientTestData.Artists.CreateArtists(10);

            Client = IntegrationTestsRuntime.CreateNormalClient();

            var bulk = new BulkRequest();
            bulk.Include(Artists.Select(i => Client.Entities.Serializer.Serialize(i)).ToArray());

            var bulkResponse = Client.Documents.BulkAsync(bulk).Result;

            foreach (var row in bulkResponse.Rows)
            {
                var artist = Artists.Single(i => i.ArtistId == row.Id);
                Client.Entities.Reflector.RevMember.SetValueTo(artist, row.Rev);
            }

            var tmp = Client.Documents.PostAsync(ClientTestData.Views.ArtistsViews).Result;

            var touchView1 = new QueryViewRequest(ClientTestData.Views.ArtistsAlbumsViewId).Configure(q => q.Stale(Stale.UpdateAfter));
            var touchView2 = new QueryViewRequest(ClientTestData.Views.ArtistsNameNoValueViewId).Configure(q => q.Stale(Stale.UpdateAfter));
            var touchView3 = new QueryViewRequest(ClientTestData.Views.ArtistsTotalNumOfAlbumsViewId).Configure(q => q.Stale(Stale.UpdateAfter));

            Client.Views.QueryAsync(touchView1).Wait();
            Client.Views.QueryAsync(touchView2).Wait();
            Client.Views.QueryAsync(touchView3).Wait();
        }

        public virtual void Dispose()
        {
            Client.ClearAllDocuments();
            Client.Dispose();
            Client = null;
        }
    }
}