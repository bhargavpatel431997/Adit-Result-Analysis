using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ARAFFinal.Startup))]
namespace ARAFFinal
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
