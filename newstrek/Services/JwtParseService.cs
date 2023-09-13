using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace newstrek.Services
{
    public class JwtParseService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtParseService (IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string ParseEmail ()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string authorizationHeader = httpContext.Request.Headers["Authorization"];
            if (authorizationHeader != null && authorizationHeader.StartsWith("Bearer "))
            {
                var userIdentity = httpContext.User.Identity as ClaimsIdentity;
                string email = userIdentity.FindFirst(ClaimTypes.Email)?.Value;

                return email;
            }

            return "Email claim is missing or invalid";
        }

        public long ParseUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string authorizationHeader = httpContext.Request.Headers["Authorization"];
            if (authorizationHeader != null && authorizationHeader.StartsWith("Bearer "))
            {
                // extracts the JWT token from the header by removing the first 7 characters ("Bearer ")
                string jwtToken = authorizationHeader.Substring(7);

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtTokenObject = tokenHandler.ReadJwtToken(jwtToken);

                // Extract the user id involved in JWT
                var userIdClaim = jwtTokenObject.Claims.Where(c => c.Type.Contains("nameidentifier")).Select(s => s.Value).ToList();
                Console.WriteLine(long.Parse(userIdClaim[0]));

                return long.Parse(userIdClaim[0]);
            }

            return 0;
        }

        public string ParseUsername ()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userIdentity = httpContext.User.Identity as ClaimsIdentity;
            string name = userIdentity.FindFirst("name")?.Value;

            return name;
        }
    }
}
