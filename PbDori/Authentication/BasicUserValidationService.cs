using AspNetCore.Authentication.Basic;
using Microsoft.Extensions.Options;

namespace PbDori.Authentication
{
    public class BasicUserValidationService : IBasicUserValidationService
    {
        private readonly ILogger<BasicUserValidationService> m_logger;
        private readonly IOptions<BasicUserValidationServiceOptions> m_options;

        public BasicUserValidationService(IOptions<BasicUserValidationServiceOptions> options,
            ILogger<BasicUserValidationService> logger)
        {
            m_logger = logger;
            m_options = options;
        }

        public Task<bool> IsValidAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return Task.FromResult(false);
                bool validLogin = string.Equals(username, m_options.Value.Username) &&
                                  string.Equals(password, m_options.Value.Password);
                return Task.FromResult(validLogin);
            }
            catch (Exception e)
            {
                m_logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
