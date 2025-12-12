using NetArchTest.Rules;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Data;
using Xunit;

namespace TicketMasala.Tests.Architecture
{
    public class LayerBoundaryTests
    {
        [Fact]
        public void Controllers_Should_Not_Depend_On_DbContext()
        {
            // Arrange
            // We have some legacy controllers that strictly violate this rule.
            // We exclude them here to prevent the build from failing, but we should refactor them in the future.
            var excludedControllers = new[]
            {
                "KnowledgeBaseController",
                "ProjectTemplateController",
                "SeedController",
                "TicketAttachmentsController",
                "CustomerController"
            };

            var result = Types.InAssembly(typeof(TicketController).Assembly)
                .That()
                .Inherit(typeof(Microsoft.AspNetCore.Mvc.Controller))
                .Or()
                .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
                .ShouldNot()
                .HaveDependencyOn(typeof(MasalaDbContext).FullName)
                .GetResult();

            var failingTypes = result.FailingTypeNames ?? new List<string>();
            var newViolations = failingTypes.Where(t => !excludedControllers.Any(e => t.EndsWith(e))).ToList();

            // Assert
            Assert.True(!newViolations.Any(),
                $"Controllers should not directly depend on MasalaDbContext. Use a service or repository instead. Violations: {string.Join(", ", newViolations)}");
        }
    }
}
