namespace GameSphere_backend.Services
{
    /// <summary>
    /// Creates or promotes the administrator configured for the application.
    /// </summary>
    public sealed class InitialAdminBootstrapper
    {
        private readonly InitialAdminConfiguration _configuration;
        private readonly InitialAdminProvisioner _provisioner;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitialAdminBootstrapper"/> class.
        /// </summary>
        public InitialAdminBootstrapper(
            InitialAdminConfiguration configuration,
            InitialAdminProvisioner provisioner)
        {
            _configuration = configuration;
            _provisioner = provisioner;
        }

        /// <summary>
        /// Ensures that the configured user exists and has the administrator role.
        /// </summary>
        public async Task EnsureAdminAsync(CancellationToken cancellationToken = default) =>
            await _provisioner.EnsureAdminAsync(_configuration, cancellationToken);
    }
}
