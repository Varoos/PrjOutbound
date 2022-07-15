using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PrjOutbound.Startup))]
namespace PrjOutbound
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //ConfigureAuth(app);
        }
    }
}
