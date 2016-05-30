// Licensed under the Apache 2.0 License. See LICENSE.txt in the project root for more information.

using System;
using ElasticLinq.Response.Materializers;
using System.Collections.Generic;
using System.Linq;
using ElasticLinq.Response.Model;
using Xunit;

namespace ElasticLinq.Test.Response.Materializers
{
    public class ManyHitsElasticMaterializerTests
    {
        [Fact]
        public void ManyOfTMaterializesObjects()
        {
            var hitCount = 3;
            var hits = MaterializerTestHelper.CreateSampleHits(hitCount);
            var materialized = ListHitsElasticMaterializer.Many<SampleClass>(hits, MaterializerTestHelper.ItemCreator).ToList();

            Assert.Equal(hitCount, materialized.Count);
            var index = 0;
            foreach (var hit in hits)
                Assert.Equal(hit.fields["someField"], materialized[index++].SampleField);
        }

        [Fact]
        public void ManyMaterializesObjects()
        {
            var hitCount = 10;
            var response = MaterializerTestHelper.CreateSampleResponse(hitCount);
            var expected = response.hits.hits;

            var materializer = new ListHitsElasticMaterializer(MaterializerTestHelper.ItemCreator, typeof(SampleClass));
            var actual = materializer.Materialize(response);

            var actualList = Assert.IsAssignableFrom<IEnumerable<SampleClass>>(actual).ToList();

            Assert.Equal(hitCount, actualList.Count);
            var index = 0;
            foreach (var hit in expected)
                Assert.Equal(hit.fields["someField"], actualList[index++].SampleField);
        }

        [Fact]
        public void MaterializeThrowsArgumentNullExceptionWhenElasticResponseIsNull()
        {
            var materializer = new ListHitsElasticMaterializer(MaterializerTestHelper.ItemCreator, typeof(SampleClass));

            Assert.Throws<ArgumentNullException>(() => materializer.Materialize(null));
        }

        [Fact]
        public void MaterializeReturnsEmptyListWhenHitsIsNull()
        {
            var materializer = new ListHitsElasticMaterializer(MaterializerTestHelper.ItemCreator, typeof(SampleClass));
            var response = new ElasticResponse { hits = null };

            var materialized = materializer.Materialize(response);

            var materializedList = Assert.IsType<List<SampleClass>>(materialized);
            Assert.Empty(materializedList);
        }

        [Fact]
        public void MaterializeReturnsEmptyListWhenHitsHitsAreNull()
        {
            var materializer = new ListHitsElasticMaterializer(MaterializerTestHelper.ItemCreator, typeof(SampleClass));
            var response = new ElasticResponse { hits = new Hits { hits = null } };

            var materialized = materializer.Materialize(response);

            var materializedList = Assert.IsType<List<SampleClass>>(materialized);
            Assert.Empty(materializedList);
        }

        [Fact]
        public void MaterializeReturnsEmptyListWhenHitsHitsAreEmpty()
        {
            var materializer = new ListHitsElasticMaterializer(MaterializerTestHelper.ItemCreator, typeof(SampleClass));
            var response = new ElasticResponse { hits = null };

            var materialized = materializer.Materialize(response);

            var materializedList = Assert.IsType<List<SampleClass>>(materialized);
            Assert.Empty(materializedList);
        }
    }
}