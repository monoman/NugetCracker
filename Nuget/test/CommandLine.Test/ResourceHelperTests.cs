using System;
using Xunit;

namespace NuGet.Test.NuGetCommandLine
{

    public class ResourceHelperTests
    {

        [Fact]
        public void GetLocalizedString_ThrowsArgumentExceptionForNullType()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => ResourceHelper.GetLocalizedString(null, "foo"), "resourceType");
        }

        [Fact]
        public void GetLocalizedString_ThrowsArgumentExceptionForNullName()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => ResourceHelper.GetLocalizedString(typeof(string), null), "resourceName");
        }

        [Fact]
        public void GetLocalizedString_ThrowsArgumentExceptionForEmptyName()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => ResourceHelper.GetLocalizedString(typeof(string), ""), "resourceName");
        }

        [Fact]
        public void GetLocalizedString_ThrowsIfNoPropteryByResourceName()
        {
            // Arrange 
            Type resourceType = typeof(MockResourceType);
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => ResourceHelper.GetLocalizedString(resourceType, "DoesntExist"),
                "The resource type 'NuGet.Test.NuGetCommandLine.ResourceHelperTests+MockResourceType' does not have an accessible static property named 'DoesntExist'.");
        }

        [Fact]
        public void GetLocalizedString_ThrowsIfPropertyIsNotOfStringType()
        {
            // Arrange 
            Type resourceType = typeof(MockResourceType);
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => ResourceHelper.GetLocalizedString(resourceType, "NotValid"),
                "The property 'NotValid' on resource type 'NuGet.Test.NuGetCommandLine.ResourceHelperTests+MockResourceType' is not a string type.");
        }

        [Fact]
        public void GetLocalizedString_ThrowsIfGetPropertyIsNotAvalible()
        {
            // Arrange 
            Type resourceType = typeof(MockResourceType);
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => ResourceHelper.GetLocalizedString(resourceType, "NoGet"),
                "The resource type 'NuGet.Test.NuGetCommandLine.ResourceHelperTests+MockResourceType' does not have an accessible get for the 'NoGet' property.");
        }

        [Fact]
        public void GetLocalizedString_ReturnsResourceWithValidName()
        {
            // Arrange
            Type resourceType = typeof(MockResourceType);
            // Act
            var actual = ResourceHelper.GetLocalizedString(resourceType, "Message");
            // Assert
            Assert.Equal("This is a Message.", actual);
        }

        private class MockResourceType
        {
            public static string Message { get { return "This is a Message."; } }
            public static string MessageTwo { get { return "This is Message Two."; } }
            public static int NotValid { get { return 0; } }
            public static string NoGet { private get { return "No Public Get."; } set { } }
        }
    }
}
