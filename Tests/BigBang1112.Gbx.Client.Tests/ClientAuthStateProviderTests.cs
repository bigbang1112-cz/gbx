namespace BigBang1112.Gbx.Client.Tests;

public class ClientAuthStateProviderTests
{
    [Fact]
    public void ClaimStringsToClaims_ReturnsEmptyEnumerable_WhenInputIsEmpty()
    {
        // Arrange
        var input = new Dictionary<string, List<string>>();

        // Act
        var result = ClientAuthStateProvider.ClaimStringsToClaims(input);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ClaimStringsToClaims_ReturnsCorrectNumberOfClaims_WhenInputHasMultipleKeyValues()
    {
        // Arrange
        var input = new Dictionary<string, List<string>>
        {
            { "type1", new List<string> { "value1", "value2" } },
            { "type2", new List<string> { "value3" } },
            { "type3", new List<string> { "value4", "value5", "value6" } }
        };

        // Act
        var result = ClientAuthStateProvider.ClaimStringsToClaims(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.Count());
    }

    [Fact]
    public void ClaimStringsToClaims_ReturnsCorrectClaims_WhenInputHasMultipleKeyValues()
    {
        // Arrange
        var input = new Dictionary<string, List<string>>
        {
            { "type1", new List<string> { "value1", "value2" } },
            { "type2", new List<string> { "value3" } }
        };

        // Act
        var claims = ClientAuthStateProvider.ClaimStringsToClaims(input);

        // Assert
        Assert.NotNull(claims);
        Assert.Equal(expected: 3, actual: claims.Count());
        Assert.Equal(expected: "type1", actual: claims.ElementAt(0).Type);
        Assert.Equal(expected: "value1", actual: claims.ElementAt(0).Value);
        Assert.Equal(expected: "type1", actual: claims.ElementAt(1).Type);
        Assert.Equal(expected: "value2", actual: claims.ElementAt(1).Value);
        Assert.Equal(expected: "type2", actual: claims.ElementAt(2).Type);
        Assert.Equal(expected: "value3", actual: claims.ElementAt(2).Value);
    }
}
