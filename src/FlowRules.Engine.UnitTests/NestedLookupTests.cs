using System;
using System.Collections.Generic;

using Xunit;

namespace FlowRules.Engine.UnitTests
{
    public class NestedLookupTests
    {
        [Fact]
        public void CanLookupSingleKeyValue()
        {
            NestedLookup<string, object> lookups = new(
                [
                    (["Key1"], 123),
                    (["Key2"], 456)
                ]
            );

            Assert.Equal(123, lookups["Key1"]);
            Assert.Equal(456, lookups["Key2"]);
        }

        [Fact]
        public void CanLookupTwoKeyValue()
        {
            NestedLookup<string, object> lookups = new(
                [
                    (["Key11", "Key12"], 123),
                    (["Key21", "Key22"], 456)
                ]
            );

            Assert.Equal(123, lookups["Key11"]["Key12"]);
            Assert.Equal(456, lookups["Key21"]["Key22"]);
        }

        [Fact]
        public void CanLookupThreeKeyValue()
        {
            NestedLookup<string, object> lookups = new(
                [
                    (["Key11", "Key12", "Key13"], 123),
                    (["Key21", "Key22", "Key23"], 456)
                ]
            );

            Assert.Equal(123, lookups["Key11"]["Key12"]["Key13"]);
            Assert.Equal(456, lookups["Key21"]["Key22"]["Key23"]);
        }

        [Fact]
        public void CanLookupAndValidateMultiLevelKeys()
        {
            NestedLookup<string, object> lookups = new(
                [
                    // First Time Buyer rules
                    (["Default", "FTB", "MinLoan"], 100_000),
                    (["Default", "FTB", "MaxLoan"], 420_000),
                    (["Default", "FTB", "MinApplicantAge"], 25),
                    (["Default", "FTB", "MinLTV"], 95.0),
                    (["Default", "FTB", "MinDSCR"], 50),
                    (["Default", "FTB", "InterestRateDSCR"], 0.95),
                    // Buy to Let rules
                    (["Default", "BTL", "MinLoan"], 200_000),
                    (["Default", "BTL", "MaxLoan"], 2_000_000),
                    (["Default", "BTL", "MinApplicantAge"], 30),
                    (["Default", "BTL", "MinLTV"], 75.0),
                    (["Default", "BTL", "MinDSCR"], 50),
                    (["Default", "BTL", "InterestRateDSCR"], 0.95),
                    (["Default", "BTL", "String"], "test"),
                ]
            );

            Assert.Equal(100_000, lookups["Default"]["FTB"]["MinLoan"]);
            Assert.Equal(0, lookups["Default"]["FTB"]["MinLoanxxx"]);
            Assert.Equal("test", lookups["Default"]["BTL"]["String"]);
            
            Assert.False(lookups["Default"].IsDefined("XXX"));
            Assert.True(lookups["Default"].IsDefined("BTL"));
        }

        [Fact]
        public void IsDefined_ReturnsFalse_AfterReadingMissingKey()
        {
            NestedLookup<string, object> lookups = new(
                [
                    (["Default", "FTB", "MinLoan"], 100_000),
                ]
            );

            _ = lookups["Default"]["FTB"]["NonExistentKey"];

            Assert.False(lookups["Default"]["FTB"].IsDefined("NonExistentKey"));
        }

        [Fact]
        public void MissingKey_ReturnsDefaultValue_WithoutThrowingException()
        {
            NestedLookup<string, object> lookups = new(
                [
                    (["Default", "FTB", "MinLoan"], 100_000),
                ]
            );

            int result = lookups["Default"]["FTB"]["NonExistentKey"];

            Assert.Equal(0, result);
        }

        [Fact]
        public void IsDefined_ReturnsFalse_ForChainedMissingKeys()
        {
            NestedLookup<string, object> lookups = new(
                [
                    (["Default", "FTB", "MinLoan"], 100_000),
                ]
            );

            _ = lookups["Default"]["Missing"]["Key"];

            Assert.False(lookups["Default"].IsDefined("Missing"));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Items_IsNull()
        {
            IEnumerable<(IEnumerable<string> Keys, object Value)>? nullItems = null;
            Assert.Throws<ArgumentNullException>(() => new NestedLookup<string, object>(nullItems!));
        }

        [Fact]
        public void Constructor_Should_Throw_When_KeySequence_IsEmpty()
        {
            Assert.Throws<ArgumentException>(() => new NestedLookup<string, object>(
                [([], 123)]
            ));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Key_InSequence_IsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NestedLookup<string, object>(
                [([null!], 123)]
            ));
        }

        [Fact]
        public void Indexer_Should_Throw_When_Key_IsNull()
        {
            NestedLookup<string, object> lookups = new(
                [(["Key1"], 123)]
            );

            Assert.Throws<ArgumentNullException>(() => _ = lookups[null!]);
        }

        [Fact]
        public void IsDefined_Should_Throw_When_Key_IsNull()
        {
            NestedLookup<string, object> lookups = new(
                [(["Key1"], 123)]
            );

            Assert.Throws<ArgumentNullException>(() => lookups.IsDefined(null!));
        }

        [Fact]
        public void ImplicitConversion_ToDouble_ReturnsExpectedValue()
        {
            NestedLookup<string, object> lookups = new(
                [(["Key1"], 3.14)]
            );

            double result = lookups["Key1"];

            Assert.Equal(3.14, result, precision: 10);
        }

        [Fact]
        public void ImplicitConversion_ToDecimal_ReturnsExpectedValue()
        {
            NestedLookup<string, object> lookups = new(
                [(["Key1"], 9.99m)]
            );

            decimal result = lookups["Key1"];

            Assert.Equal(9.99m, result);
        }

        [Fact]
        public void ImplicitConversion_ToBool_ReturnsExpectedValue()
        {
            NestedLookup<string, object> lookups = new(
                [(["Key1"], true)]
            );

            bool result = lookups["Key1"];

            Assert.True(result);
        }

        [Fact]
        public void MissingKey_ReturnsCustomDefaultValue_FromFactory()
        {
            NestedLookup<string, int> lookups = new(
                [(["Key1"], 100)],
                defaultValueFactory: () => -1
            );

            int result = lookups["Missing"];

            Assert.Equal(-1, result);
        }
    }
}
