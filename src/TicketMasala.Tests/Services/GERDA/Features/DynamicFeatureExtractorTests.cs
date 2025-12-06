using TicketMasala.Web.Models;
using TicketMasala.Web.Models.Configuration;
using TicketMasala.Web.Engine.GERDA.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace TicketMasala.Tests.Services.GERDA.Features;
    public class DynamicFeatureExtractorTests
    {
        private readonly DynamicFeatureExtractor _extractor;

        public DynamicFeatureExtractorTests()
        {
            _extractor = new DynamicFeatureExtractor(new NullLogger<DynamicFeatureExtractor>());
        }

        [Fact]
        public void ExtractFeatures_NumericMinMax_ScalesCorrectly()
        {
            // Arrange
            var ticket = new Ticket
            {
                CustomFieldsJson = "{\"soil_ph\": 7.0}",
                TicketStatus = Status.Pending,
                Description = "Test Ticket",
                Customer = new ApplicationUser { Id = "C1", FirstName = "Test", LastName = "Customer", Email = "test@example.com", Phone = "1234567890", UserName = "test@example.com" }
            };

            var config = new GerdaModelConfig
            {
                Features = new List<FeatureDefinition>
                {
                    new FeatureDefinition
                    {
                        Name = "soil_ph_norm",
                        SourceField = "soil_ph",
                        Transformation = "min_max",
                        Params = new Dictionary<string, object>
                        {
                            { "min", 0 },
                            { "max", 14 }
                        }
                    }
                }
            };

            // Act
            var vector = _extractor.ExtractFeatures(ticket, config);

            // Assert
            Assert.Single(vector);
            Assert.Equal(0.5f, vector[0]); // 7 / 14 = 0.5
        }

        [Fact]
        public void ExtractFeatures_OneHot_MatchesTarget()
        {
            // Arrange
            var ticket = new Ticket
            {
                CustomFieldsJson = "{\"zone\": \"Z1\"}",
                TicketStatus = Status.Pending,
                Description = "Test Ticket",
                Customer = new ApplicationUser { Id = "C1", FirstName = "Test", LastName = "Customer", Email = "test@example.com", Phone = "1234567890", UserName = "test@example.com" }
            };

            var config = new GerdaModelConfig
            {
                Features = new List<FeatureDefinition>
                {
                    new FeatureDefinition
                    {
                        Name = "is_zone_1",
                        SourceField = "zone",
                        Transformation = "one_hot",
                        Params = new Dictionary<string, object>
                        {
                            { "target_value", "Z1" }
                        }
                    }
                }
            };

            // Act
            var vector = _extractor.ExtractFeatures(ticket, config);

            // Assert
            Assert.Single(vector);
            Assert.Equal(1.0f, vector[0]);
        }

        [Fact]
        public void ExtractFeatures_OneHot_Mismatch_ReturnsZero()
        {
            // Arrange
            var ticket = new Ticket
            {
                CustomFieldsJson = "{\"zone\": \"Z2\"}",
                TicketStatus = Status.Pending,
                Description = "Test Ticket",
                Customer = new ApplicationUser { Id = "C1", FirstName = "Test", LastName = "Customer", Email = "test@example.com", Phone = "1234567890", UserName = "test@example.com" }
            };

            var config = new GerdaModelConfig
            {
                Features = new List<FeatureDefinition>
                {
                    new FeatureDefinition
                    {
                        Name = "is_zone_1",
                        SourceField = "zone",
                        Transformation = "one_hot",
                        Params = new Dictionary<string, object>
                        {
                            { "target_value", "Z1" }
                        }
                    }
                }
            };

            // Act
            var vector = _extractor.ExtractFeatures(ticket, config);

            // Assert
            Assert.Single(vector);
            Assert.Equal(0.0f, vector[0]);
        }
}
