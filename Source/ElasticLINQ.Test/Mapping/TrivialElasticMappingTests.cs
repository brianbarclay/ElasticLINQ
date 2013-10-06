﻿// Copyright (c) Tier 3 Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 

using ElasticLinq.Mapping;
using System.Reflection;
using Xunit;

namespace ElasticLINQ.Test.Mapping
{
    public class TrivialElasticMappingTests
    {
        [Fact]
        public void GetFieldNameCamelCasesMemberName()
        {
            var memberInfo = MethodBase.GetCurrentMethod();

            var mapping = new TrivialElasticMapping();
            var actual = mapping.GetFieldName(memberInfo);

            Assert.Equal("getFieldNameCamelCasesMemberName", actual);
        }

        [Fact]
        public void GetTypeNameCamelCasesAndPluralizesSingularTypeName()
        {
            var type = typeof(SingularTypeName);

            var mapping = new TrivialElasticMapping();
            var actual = mapping.GetTypeName(type);

            Assert.Equal("singularTypeNames", actual);
        }

        private class SingularTypeName { }

        [Fact]
        public void GetTypeNameCamelCasesPluralTypeName()
        {
            var type = typeof(SingularTypeNames);

            var mapping = new TrivialElasticMapping();
            var actual = mapping.GetTypeName(type);

            Assert.Equal("singularTypeNames", actual);
        }

        private class SingularTypeNames { }
    }
}