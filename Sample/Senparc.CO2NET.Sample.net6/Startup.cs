using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Cache.Memcached;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Sample.net6.Services;
using Senparc.CO2NET.WebApi;
using Senparc.CO2NET.WebApi.WebApiEngines;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;

namespace Senparc.CO2NET.Sample.net6
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddMemoryCache();//ʹ�ñ��ػ���Ҫ���
            services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));//ʹ�� Memcached �� Logger ��Ҫ���
            var builder = services.AddMvcCore();
            //Senparc.CO2NET ȫ��ע�ᣨ���룩
            services.AddSenparcGlobalServices(Configuration);

            //Senparc.NeuChar.Register.AddNeuChar();


            var appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "App_Data");
            services.AddAndInitDynamicApi(builder, appDataPath, ApiRequestMethod.Get, null, 400, false, true, m => null);

            #region ��������
            services.AddScoped(typeof(ApiBindTestService));
            services.AddScoped(typeof(EntityApiBindTestService));
            var apiBindTestService = new ApiBindTestService();
            apiBindTestService.DynamicBuild(services, builder);
            #endregion

            #region Swagger



            //.NET Core 3.0 for Swagger https://www.thecodebuzz.com/swagger-api-documentation-in-net-core-3-0/


            //���Swagger
            services.AddSwaggerGen(c =>
            {
                //Ϊÿ�����򼯴����ĵ�
                foreach (var neucharApiDocAssembly in WebApiEngine.ApiAssemblyCollection)
                {
                    var version = WebApiEngine.ApiAssemblyVersions[neucharApiDocAssembly.Key]; //neucharApiDocAssembly.Value.ImageRuntimeVersion;
                    var docName = WebApiEngine.GetDocName(neucharApiDocAssembly.Key);
                    c.SwaggerDoc(docName, new OpenApiInfo
                    {
                        Title = $"CO2NET Dynamic WebApi Engine : {neucharApiDocAssembly.Key}",
                        Version = $"v{version}",//"v16.5.4"
                        Description = $"Senparc CO2NET WebApi ��̬���棨{neucharApiDocAssembly.Key} - v{version}��",
                        //License = new OpenApiLicense()
                        //{
                        //    Name = "Apache License Version 2.0",
                        //    Url = new Uri("https://github.com/JeffreySu/WeiXinMPSDK")
                        //},
                        Contact = new OpenApiContact()
                        {
                            Email = "zsu@senparc.com",
                            Name = "Senparc Team",
                            Url = new Uri("https://www.senparc.com")
                        },
                        //TermsOfService = new Uri("https://github.com/JeffreySu/WeiXinMPSDK")
                    });

                    //if (neucharApiDocAssembly.Key.Contains("WeChat"))
                    //{
                    //    c.IncludeXmlComments($"App_Data/ApiDocXml/{WebApiEngine.WeixinApiAssemblyNames[neucharApiDocAssembly.Key]}.xml");
                    //}
                }

                ////������ʾ  https://www.cnblogs.com/toiv/archive/2018/07/28/9379249.html
                //c.DocInclusionPredicate((docName, apiDesc) =>
                //{
                //    if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo))
                //    {
                //        return false;
                //    }

                //    var versions = methodInfo.DeclaringType
                //          .GetCustomAttributes(true)
                //          .OfType<SwaggerOperationAttribute>()
                //          .Select(z => z.Tags[0].Split(':')[0]);

                //    if (versions.FirstOrDefault() == null)
                //    {
                //        return false;//������Ҫ��Ķ�����ʾ
                //    }

                //    //docName: $"{neucharApiDocAssembly.Key}-v1"
                //    return versions.Any(z => docName.StartsWith(z));
                //});

                c.OrderActionsBy(z => z.RelativePath);
                //c.DescribeAllEnumsAsStrings();//ö����ʾ�ַ���
                c.EnableAnnotations();
                c.DocumentFilter<RemoveVerbsFilter>();
                c.CustomSchemaIds(x => x.FullName);//��ܴ���InvalidOperationException: Can't use schemaId "$JsApiTicketResult" for type "$Senparc.Weixin.Open.Entities.JsApiTicketResult". The same schemaId was already used for type "$Senparc.Weixin.MP.Entities.JsApiTicketResult"

                /* ��Ҫ��½���ݲ�����    ���� Jeffrey Su 2021.06.17
                var oAuthDocName = "oauth2";// WeixinApiService.GetDocName(PlatformType.WeChat_OfficialAccount);

                //�����Ȩ
                var authorizationUrl = NeuChar.App.AppStore.Config.IsDebug
                                               //������ appPurachase �� Id��ʵ��Ӧ���� appId
                                               //? "http://localhost:12222/App/LoginOAuth/Authorize/1002/"
                                               //: "https://www.neuchar.com/App/LoginOAuth/Authorize/4664/";
                                               //��������ȷ�� appId
                                               ? "http://localhost:12222/App/LoginOAuth/Authorize?appId=xxx"
                                               : "https://www.neuchar.com/App/LoginOAuth/Authorize?appId=3035";

                c.AddSecurityDefinition(oAuthDocName,//"Bearer" 
                    new OpenApiSecurityScheme
                    {
                        Description = "���������Bearer��ͷ��Token",
                        Name = oAuthDocName,// "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.OAuth2,
                        //OpenIdConnectUrl = new Uri("https://www.neuchar.com/"),
                        Flows = new OpenApiOAuthFlows()
                        {
                            Implicit = new OpenApiOAuthFlow()
                            {
                                AuthorizationUrl = new Uri(authorizationUrl),
                                Scopes = new Dictionary<string, string> { { "swagger_api", "Demo API - full access" } }
                            }
                        }
                    });

                //��֤��ʽ���˷�ʽΪȫ�����
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    { new OpenApiSecurityScheme(){ Name =oAuthDocName//"Bearer"
                    }, new List<string>() }
                    //{ "Bearer", Enumerable.Empty<string>() }
                });

                //c.OperationFilter<AuthResponsesOperationFilter>();//AuthorizeAttribute����

                */

            });
            #endregion

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<SenparcSetting> senparcSetting)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthorization();

            // ���� CO2NET ȫ��ע�ᣬ���룡
            app.UseSenparcGlobal(env, senparcSetting.Value, register =>
                {
                    #region CO2NET ȫ������

                    #region ȫ�ֻ������ã����裩

                    //��ͬһ���ֲ�ʽ����ͬʱ�����ڶ����վ��Ӧ�ó���أ�ʱ������ʹ�������ռ佫����루�Ǳ��룩
                    register.ChangeDefaultCacheNamespace("CO2NETCache.net6.0");

                    #region ���ú�ʹ�� Redis

                    //����ȫ��ʹ��Redis���棨���裬������
                    var redisConfigurationStr = senparcSetting.Value.Cache_Redis_Configuration;
                    var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "Redis����";
                    if (useRedis)//����Ϊ�˷��㲻ͬ�����Ŀ����߽������ã��������жϵķ�ʽ��ʵ�ʿ�������һ����ȷ���ģ������if�������Ժ���
                    {
                        /* ˵����
                         * 1��Redis �������ַ�����Ϣ��� Config.SenparcSetting.Cache_Redis_Configuration �Զ���ȡ��ע�ᣬ�粻��Ҫ�޸ģ��·��������Ժ���
                        /* 2�������ֶ��޸ģ�����ͨ���·� SetConfigurationOption �����ֶ����� Redis ������Ϣ�����޸����ã����������ã�
                         */
                        Senparc.CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);

                        //���»�������ȫ�ֻ�������Ϊ Redis
                        Senparc.CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow();//��ֵ�Ի�����ԣ��Ƽ���
                        //Senparc.CO2NET.Cache.Redis.Register.UseHashRedisNow();//HashSet�����ʽ�Ļ������

                        //Ҳ����ͨ�����·�ʽ�Զ��嵱ǰ��Ҫ���õĻ������
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//��ֵ��
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
                    }
                    //������ﲻ����Redis�������ã���Ŀǰ����Ĭ��ʹ���ڴ滺�� 

                    #endregion

                    #region ���ú�ʹ�� Memcached

                    //����Memcached���棨���裬������
                    var memcachedConfigurationStr = senparcSetting.Value.Cache_Memcached_Configuration;
                    var useMemcached = !string.IsNullOrEmpty(memcachedConfigurationStr) && memcachedConfigurationStr != "Memcached����";

                    if (useMemcached) //����Ϊ�˷��㲻ͬ�����Ŀ����߽������ã��������жϵķ�ʽ��ʵ�ʿ�������һ����ȷ���ģ������if�������Ժ���
                    {
                        app.UseEnyimMemcached();

                        /* ˵����
                        * 1��Memcached �������ַ�����Ϣ��� Config.SenparcSetting.Cache_Memcached_Configuration �Զ���ȡ��ע�ᣬ�粻��Ҫ�޸ģ��·��������Ժ���
                       /* 2�������ֶ��޸ģ�����ͨ���·� SetConfigurationOption �����ֶ����� Memcached ������Ϣ�����޸����ã����������ã�
                        */
                        Senparc.CO2NET.Cache.Memcached.Register.SetConfigurationOption(redisConfigurationStr);

                        //���»�������ȫ�ֻ�������Ϊ Memcached
                        Senparc.CO2NET.Cache.Memcached.Register.UseMemcachedNow();

                        //Ҳ����ͨ�����·�ʽ�Զ��嵱ǰ��Ҫ���õĻ������
                        CacheStrategyFactory.RegisterObjectCacheStrategy(() => MemcachedObjectCacheStrategy.Instance);
                    }

                    #endregion

                    #endregion

                    #region ע����־�����裬���飩

                    register.RegisterTraceLog(ConfigTraceLog);//����TraceLog

                    #endregion

                    #endregion
                },

            #region ɨ���Զ�����չ����

                //�Զ�ɨ���Զ�����չ���棨��ѡһ��
                autoScanExtensionCacheStrategies: true //Ĭ��Ϊ true�����Բ�����
                                                       //ָ���Զ�����չ���棨��ѡһ��
                                                       //autoScanExtensionCacheStrategies: false, extensionCacheStrategiesFunc: () => GetExCacheStrategies(senparcSetting.Value)

            #endregion
            );

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //c.DocumentTitle = "Senparc Weixin SDK Demo API";
                c.InjectJavascript("/lib/jquery/dist/jquery.min.js");
                c.InjectJavascript("/js/swagger.js");
                //c.InjectJavascript("/js/tongji.js");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);

                foreach (var co2netApiDocAssembly in WebApiEngine.ApiAssemblyCollection)
                {

                    //TODO:��ʵ�Ķ�̬�汾��
                    var verion = WebApiEngine.ApiAssemblyVersions[co2netApiDocAssembly.Key]; //neucharApiDocAssembly.Value.ImageRuntimeVersion;
                    var docName = WebApiEngine.GetDocName(co2netApiDocAssembly.Key);

                    //Console.WriteLine($"\tAdd {docName}");

                    c.SwaggerEndpoint($"/swagger/{docName}/swagger.json", $"{co2netApiDocAssembly.Key} v{verion}");
                }

                //OAuth     https://www.cnblogs.com/miskis/p/10083985.html
                c.OAuthClientId("e65ea785b96b442a919965ccf857aba3");//�ͷ�������
                c.OAuthAppName("΢�� API Swagger �ĵ� "); // ����
            });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

        }

        /// <summary>
        /// ����ȫ�ָ�����־
        /// </summary>
        private void ConfigTraceLog()
        {
            //������ΪDebug״̬ʱ��/App_Data/SenparcTraceLog/Ŀ¼�»�������־�ļ���¼���е�API������־����ʽ�����汾����ر�

            //���ȫ�ֵ�IsDebug��Senparc.CO2NET.Config.IsDebug��Ϊfalse���˴����Ե�������true�������Զ�Ϊtrue
            CO2NET.Trace.SenparcTrace.SendCustomLog("ϵͳ��־", "ϵͳ����");//ֻ��Senparc.CO2NET.Config.IsDebug = true���������Ч

            //ȫ���Զ�����־��¼�ص�
            CO2NET.Trace.SenparcTrace.OnLogFunc = () =>
            {
                //����ÿ�δ���Log����Ҫִ�еĴ���
            };

            CO2NET.Trace.SenparcTrace.OnBaseExceptionFunc = ex =>
            {
                //����ÿ�δ���BaseException����Ҫִ�еĴ���
            };
        }

        /// <summary>
        /// ��ȡ��չ�������
        /// </summary>
        /// <returns></returns>
        private IList<IDomainExtensionCacheStrategy> GetExCacheStrategies(SenparcSetting senparcSetting)
        {
            var exContainerCacheStrategies = new List<IDomainExtensionCacheStrategy>();
            senparcSetting = senparcSetting ?? new SenparcSetting();

            //ע�⣺�������� if �жϽ���Ϊ��ʾ������������Զ������չ������ԣ�

            #region ��ʾ��չ����ע�᷽��

            /*

            //�ж�Redis�Ƿ����
            var redisConfiguration = senparcSetting.Cache_Redis_Configuration;
            if ((!string.IsNullOrEmpty(redisConfiguration) && redisConfiguration != "Redis����"))
            {
                exContainerCacheStrategies.Add(RedisContainerCacheStrategy.Instance);//�Զ������չ����
            }

            //�ж�Memcached�Ƿ����
            var memcachedConfiguration = senparcSetting.Cache_Memcached_Configuration;
            if ((!string.IsNullOrEmpty(memcachedConfiguration) && memcachedConfiguration != "Memcached����"))
            {
                exContainerCacheStrategies.Add(MemcachedContainerCacheStrategy.Instance);//TODO:���û�н������û�����쳣
            }
            */

            #endregion

            //��չ�Զ���Ļ������

            return exContainerCacheStrategies;
        }


        class RemoveVerbsFilter : IDocumentFilter
        {
            //public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
            //{
            //    foreach (PathItem path in swaggerDoc.paths.Values)
            //    {
            //        path.delete = null;
            //        //path.get = null; // leaving GET in
            //        path.head = null;
            //        path.options = null;
            //        path.patch = null;
            //        path.post = null;
            //        path.put = null;
            //    }
            //}

            public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
            {
                //ÿ���л����壬����Ҫ�����Ƚϳ���ʱ��ŵ�������

                return;
                string platformType;
                var title = swaggerDoc.Info.Title;

                //if (title.Contains(PlatformType.WeChat_OfficialAccount.ToString()))
                //{
                //    platformType = PlatformType.WeChat_OfficialAccount.ToString();
                //}
                //else if (title.Contains(PlatformType.WeChat_Work.ToString()))
                //{
                //    platformType = PlatformType.WeChat_Work.ToString();
                //}
                //else if (title.Contains(PlatformType.WeChat_Open.ToString()))
                //{
                //    platformType = PlatformType.WeChat_Open.ToString();
                //}
                //else if (title.Contains(PlatformType.WeChat_MiniProgram.ToString()))
                //{
                //    platformType = PlatformType.WeChat_MiniProgram.ToString();
                //}
                ////else if (title.Contains(PlatformType.General.ToString()))
                ////{
                ////    platformType = PlatformType.General.ToString();
                ////}
                //else
                //{
                //    throw new NotImplementedException($"δ�ṩ�� PlatformType ���ͣ�Title��{title}");
                //}

                //var pathList = swaggerDoc.Paths.Keys.ToList();

                //foreach (var path in pathList)
                //{
                //    if (!path.Contains(platformType))
                //    {
                //        //�Ƴ��ǵ�ǰģ���API����
                //        swaggerDoc.Paths.Remove(path);
                //    }
                //}

                //SwaggerOperationAttribute
                //�Ƴ�Schema����
                //var toRemoveSchema = context.SchemaRepository.Schemas.Where(z => !z.Key.Contains(platformType)).ToList();//���Ϊȫ��ɾ����������
                //foreach (var schema in toRemoveSchema)
                //{
                //    context.SchemaRepository.Schemas.Remove(schema.Key);
                //}
            }
        }

        //public class AuthResponsesOperationFilter : IOperationFilter
        //{
        //    public void Apply(OpenApiOperation operation, OperationFilterContext context)
        //    {
        //        //��ȡ�Ƿ���ӵ�¼����
        //        var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
        //         .Union(context.MethodInfo.GetCustomAttributes(true))
        //         .OfType<AuthorizeAttribute>().Any();

        //        if (authAttributes)
        //        {
        //            operation.Responses.Add("401", new OpenApiResponse { Description = "���޷���Ȩ��" });
        //            operation.Responses.Add("403", new OpenApiResponse { Description = "��ֹ����" });
        //            operation.Security = new List<OpenApiSecurityRequirement>
        //            {
        //                new OpenApiSecurityRequirement { { new OpenApiSecurityScheme() {  Name= "oauth2" }, new[] { "swagger_api" } }}
        //            };
        //        }
        //    }
        //}
    }
}
