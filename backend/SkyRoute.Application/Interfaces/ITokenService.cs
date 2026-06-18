using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken();
}
