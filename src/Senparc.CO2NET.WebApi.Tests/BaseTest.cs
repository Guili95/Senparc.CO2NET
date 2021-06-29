using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Senparc.CO2NET.WebApi.Tests
{
    [TestClass]
    public class BaseTest
    {
        public IServiceCollection ServiceCollection { get; }
        public IServiceProvider ServiceProvider { get; set; }
        public IConfiguration Configuration { get; set; }

        protected IRegisterService registerService;
        protected SenparcSetting _senparcSetting;

        protected Mock<Microsoft.Extensions.Hosting.IHostEnvironment/*IHostingEnvironment*/> _env;

        public BaseTest()
        {
            _env = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment/*IHostingEnvironment*/>();
            _env.Setup(z => z.ContentRootPath).Returns(() => Path.GetFullPath("..\\..\\..\\"));

            //SiteConfig.WebRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            //ServiceCollection = new ServiceCollection();
            //var result = ServiceCollection.StartEngine(Configuration);

        }

        public void Init()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            RegisterServiceCollection();
            RegisterServiceStart();
            Console.WriteLine("��� TaseTest ��ʼ��");
        }

        /// <summary>
        /// ע�� IServiceCollection �� MemoryCache
        /// </summary>
        public void RegisterServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json", false, false);
            var config = configBuilder.Build();
            Configuration = config;

            _senparcSetting = new SenparcSetting() { IsDebug = true };
            config.GetSection("SenparcSetting").Bind(_senparcSetting);

            serviceCollection.AddDatabase<SqliteMemoryDatabaseConfiguration>();//ʹ�� SQLServer���ݿ�


            SiteConfig.WebRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");

            Senparc.Ncf.Core.Register.TryRegisterMiniCore(services => { });
            serviceCollection.AddSenparcGlobalServices(config);

            serviceCollection.AddMemoryCache();//ʹ���ڴ滺��
            serviceCollection.AddRouting();
            var builder = serviceCollection.AddRazorPages();
            builder.AddNcfAreas(_env.Object);

            //�Զ�����ע��ɨ��
            serviceCollection.ScanAssamblesForAutoDI();
            //�Ѿ���������г����Զ�ɨ���ί�У�����ִ��ɨ�裨���룩
            AssembleScanHelper.RunScan();


            var result = serviceCollection.StartEngine(Configuration);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// ע�� RegisterService.Start()
        /// </summary>
        public void RegisterServiceStart(bool autoScanExtensionCacheStrategies = false)
        {
            //ע��
            registerService = Senparc.CO2NET.AspNet.RegisterServices.RegisterService.Start(_env.Object, _senparcSetting)
                .UseSenparcGlobal(autoScanExtensionCacheStrategies);

            IApplicationBuilder app = new ApplicationBuilder(ServiceProvider);
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });

            Console.WriteLine("Senparc.Ncf.XncfBase.Register.UseXncfModules");
            //XncfModules�����룩
            Senparc.Ncf.XncfBase.Register.UseXncfModules(app, registerService);
        }
    }
}
