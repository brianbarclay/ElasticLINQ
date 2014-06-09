﻿// Licensed under the Apache 2.0 License. See LICENSE.txt in the project root for more information.

using ElasticLinq.Request.Criteria;
using ElasticLinq.Request.Facets;
using ElasticLinq.Request.Visitors;
using ElasticLinq.Response.Materializers;
using System.Linq;
using Xunit;

namespace ElasticLinq.Test.Request.Visitors.ElasticQueryTranslation
{
    public class ElasticQueryTranslationGroupByFieldTests : ElasticQueryTranslationTestsBase
    {
        [Fact]
        public void SelectSumCreatesTermsStatsFacet()
        {
            var query = Robots.GroupBy(r => r.Zone).Select(g => g.Sum(r => r.Cost));

            var translation = ElasticQueryTranslator.Translate(Mapping, "", query.Expression);

            Assert.Equal(typeof(decimal), Assert.IsType<ElasticFacetsMaterializer>(translation.Materializer).ElementType);
            Assert.Equal(1, translation.SearchRequest.Facets.Count);
            var facet = Assert.IsType<TermsStatsFacet>(translation.SearchRequest.Facets[0]);
            Assert.Null(facet.Filter);
            Assert.Equal("zone", facet.Key);
            Assert.Equal("cost", facet.Value);
        }

        [Fact]
        public void SelectCoundPredicateCreatesTermsFacetWithFilter()
        {
            var query = Robots.GroupBy(r => r.Zone).Select(g => g.Count(r => r.Cost > 5m));

            var translation = ElasticQueryTranslator.Translate(Mapping, "a", query.Expression);

            Assert.Equal(typeof(int), Assert.IsType<ElasticFacetsMaterializer>(translation.Materializer).ElementType);
            Assert.Equal(1, translation.SearchRequest.Facets.Count);
            
            var facet = Assert.IsType<TermsFacet>(translation.SearchRequest.Facets[0]);
            Assert.Equal(1, facet.Fields.Count);
            Assert.Equal("a.zone", facet.Fields[0]);
            
            var filter = Assert.IsType<RangeCriteria>(facet.Filter);
            Assert.Equal("a.cost", filter.Field);
            Assert.Equal(1, filter.Specifications.Count);
            
            var specification = filter.Specifications[0];
            Assert.Equal("gt", specification.Name);
            Assert.Equal(5m, specification.Value);
        }

        [Fact]
        public void WhereWithSelectCountPredicateCreatesTwoFilters()
        {
            var grouped = Robots.Where(r => r.Zone != null).GroupBy(r => r.Zone).Select(g => g.Count(r => r.Cost == 2.0m));

            var translation = ElasticQueryTranslator.Translate(Mapping, "", grouped.Expression);

            Assert.Equal(typeof(int), Assert.IsType<ElasticFacetsMaterializer>(translation.Materializer).ElementType);
            Assert.Equal(1, translation.SearchRequest.Facets.Count);

            {
                var where = Assert.IsType<ExistsCriteria>(translation.SearchRequest.Filter);
                Assert.Equal("zone", where.Field);
            }

            var facet = Assert.IsType<TermsFacet>(translation.SearchRequest.Facets[0]);
            var facetTerm = Assert.IsType<TermCriteria>(facet.Filter);
            Assert.Equal("cost", facetTerm.Field);
            Assert.Equal(2.0m, facetTerm.Value);
        }

        [Fact]
        public void SelectAverageWithTakeCreatesTermsStatsFacetWithSize()
        {
            var query = Robots.GroupBy(r => r.Zone).Select(g => g.Average(r => r.EnergyUse)).Take(5);

            var translation = ElasticQueryTranslator.Translate(Mapping, "p", query.Expression);

            Assert.Equal(typeof(double), Assert.IsType<ElasticFacetsMaterializer>(translation.Materializer).ElementType);
            Assert.Equal(1, translation.SearchRequest.Facets.Count);

            var facet = Assert.IsType<TermsStatsFacet>(translation.SearchRequest.Facets[0]);
            Assert.Null(facet.Filter);
            Assert.Equal("p.zone", facet.Key);
            Assert.Equal("p.energyUse", facet.Value);
            Assert.Equal(5, facet.Size);
        }

        [Fact]
        public void SelectProjectionCreatesMultipleFacets()
        {
            var expectedFields = new[] { "cost", "energyUse", "started" };
            var query = Robots.GroupBy(r => r.Zone)
                .Select(g => new
                {
                    SumCost = g.Sum(a => a.Cost),
                    AverageEnergyUse = g.Average(a => a.EnergyUse),
                    MinStarted = g.Min(a => a.Started),
                });

            var translation = ElasticQueryTranslator.Translate(Mapping, "", query.Expression);

            Assert.Contains("AnonymousType", Assert.IsType<ElasticFacetsMaterializer>(translation.Materializer).ElementType.FullName);
            Assert.Equal(expectedFields.Length, translation.SearchRequest.Facets.Count);
            foreach (var expectedField in expectedFields)
            {
                var facet = translation.SearchRequest.Facets.OfType<TermsStatsFacet>().Single(s => s.Value == expectedField);
                Assert.Equal("zone", facet.Key);
                Assert.Null(facet.Filter);
            }
        }

        [Fact]
        public void SelectProjectionCombinesSameFieldToSingleFacet()
        {
            var query = Robots.GroupBy(r => r.Zone)
                .Select(g => new
                {
                    AverageEnergy = g.Average(a => a.EnergyUse),
                    MaxEnergy = g.Max(a => a.EnergyUse)
                });

            var searchRequest = ElasticQueryTranslator.Translate(Mapping, "", query.Expression).SearchRequest;

            Assert.Equal(1, searchRequest.Facets.Count);
            var facet = Assert.IsType<TermsStatsFacet>(searchRequest.Facets[0]);
            Assert.Null(facet.Filter);
            Assert.Equal("zone", facet.Key);
            Assert.Equal("energyUse", facet.Value);
        }

        [Fact]
        public void SelectProjectionCanCreateMixedFacets()
        {
            var query = Robots.GroupBy(r => r.Zone)
                .Select(g => new
                {
                    AverageEnergyUse = g.Average(a => a.EnergyUse),
                    CountHighEnergy = g.Count(a => a.EnergyUse > 50.0)
                });

            var translation = ElasticQueryTranslator.Translate(Mapping, "", query.Expression);

            Assert.Contains("AnonymousType", Assert.IsType<ElasticFacetsMaterializer>(translation.Materializer).ElementType.FullName);
            Assert.Equal(2, translation.SearchRequest.Facets.Count);

            {
                var termsStatsFacet = translation.SearchRequest.Facets.OfType<TermsStatsFacet>().Single();
                Assert.Equal("zone", termsStatsFacet.Key);
                Assert.Equal("energyUse", termsStatsFacet.Value);
                Assert.Null(termsStatsFacet.Filter);
            }

            {
                var termsFacet = translation.SearchRequest.Facets.OfType<TermsFacet>().Single();
                var range = Assert.IsType<RangeCriteria>(termsFacet.Filter);
                Assert.Equal("energyUse", range.Field);
                Assert.Equal(1, range.Specifications.Count);
                var specification = range.Specifications[0];
                Assert.Equal(RangeComparison.GreaterThan, specification.Comparison);
                Assert.Equal(50.0, specification.Value);
            }
        }
    }
}