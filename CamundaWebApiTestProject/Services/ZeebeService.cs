using Newtonsoft.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;
using Zeebe.Client.Impl.Builder;

namespace CamundaWebApiTestProject.Services
{
    public class ZeebeService : IZeebeService
    {
        private readonly IZeebeClient _client;
        private readonly ILogger<ZeebeService> _logger;

        public static string AuthServer = "https://login.cloud.camunda.io/oauth/token";
        public static string ClientId = "";
        public static string ClientSecret = "";
        public static string ZeebeUrl = "";

        public static char[] Port =
        {
            '4', '3', ':'
        };

        public ZeebeService(ILogger<ZeebeService> logger)
        {
            _logger = logger;

            string? audience = ZeebeUrl?.TrimEnd(Port);

            //Init zeebe client to connect our cluster with BPMN diagrams
            _client =
                ZeebeClient.Builder()
                    .UseGatewayAddress(ZeebeUrl)
                    .UseTransportEncryption()
                    .UseAccessTokenSupplier(
                        CamundaCloudTokenProvider.Builder()
                            .UseAuthServer(AuthServer)
                            .UseClientId(ClientId)
                            .UseClientSecret(ClientSecret)
                            .UseAudience(audience)
                            .Build())
                    .Build();
        }

        //Check status of the cluster
        public Task<ITopology> Status()
        {
            return _client.TopologyRequest().Send();
        }

        //start Process by bpmProcessId
        public async Task<string> StartWorkflowInstance(string bpmProcessId)
        {
            VehicleInfoDTO model = new()
            {
                Vehicle = "Mazda",
                Engine = 3
            };

            IProcessInstanceResult instance = await _client.NewCreateProcessInstanceCommand()
                .BpmnProcessId(bpmProcessId)
                .LatestVersion()
                .Variables(JsonConvert.SerializeObject(model))
                .WithResult()
                .Send();

            return JsonConvert.SerializeObject(instance);
        }

        //Start all workers
        public void StartWorkers()
        {
            CreateGetTimeWorker();
            CreateMakeGreetingWorker();
            CreateIsVehicleGoodWorker();
        }

        //Create worker for get-time task
        public void CreateGetTimeWorker()
        {
            string jobType = "get-time";

            _createWorker(jobType,
                async (client, job) =>
                {
                    _logger.LogInformation("Received job: " + job);

                    using (var httpClient = new HttpClient())
                    {
                        using (var response = await httpClient.GetAsync("https://json-api.joshwulf.com/time"))
                        {
                            string apiResponse = await response.Content.ReadAsStringAsync();

                            await client.NewCompleteJobCommand(job.Key)
                                .Variables("{\"time\":" + apiResponse + "}")
                                .Send();
                        }
                    }
                });
        }

        //Create worker for make-greeting task
        public void CreateMakeGreetingWorker()
        {
            string jobType = "make-greeting";

            _createWorker(jobType,
                async (client, job) =>
                {
                    _logger.LogInformation("Make Greeting Received job: " + job);
                    MakeGreetingCustomHeadersDTO? headers = JsonConvert.DeserializeObject<MakeGreetingCustomHeadersDTO>(job.CustomHeaders);
                    MakeGreetingVariablesDTO? variables = JsonConvert.DeserializeObject<MakeGreetingVariablesDTO>(job.Variables);

                    await client.NewCompleteJobCommand(job.Key)
                        .Variables("{\"say\": \"" + headers.Greeting + " " + variables.Name + "\"}")
                        .Send();

                    _logger.LogInformation("Make Greeting Worker completed job");
                });
        }

        //Create worker for get-decision-result task, get decision value
        public void CreateIsVehicleGoodWorker()
        {
            string jobType = "get-decision-result";

            _createWorker(jobType,
                async (client, job) =>
                {
                    _logger.LogInformation("Is vehicle good Received job: " + job);
                    var variables = JsonConvert.DeserializeObject<IsVehicleGoodVariablesDTO>(job.Variables);

                    await client.NewCompleteJobCommand(job.Key)
                        .Send();

                    _logger.LogInformation("Is vehicle good Worker completed job. Decision: " + variables.ResultDecision);
                });
        }

        //worker creation
        private void _createWorker(string jobType, JobHandler handleJob)
        {
            IJobWorker? createdWorker = _client.NewWorker()
                .JobType(jobType)
                .Handler(handleJob)
                .MaxJobsActive(5)
                .Name(jobType)
                .PollInterval(TimeSpan.FromSeconds(50))
                .PollingTimeout(TimeSpan.FromSeconds(50))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();
        }
    }

    //DTO to exchange with Camunda
    public class MakeGreetingCustomHeadersDTO
    {
        public string Greeting { get; set; }
    }

    public class MakeGreetingVariablesDTO
    {
        public string Name { get; set; }
    }

    public class IsVehicleGoodVariablesDTO
    {
        public string Vehicle { get; set; }
        public int Engine { get; set; }

        [JsonProperty(PropertyName = "resultDecision")]
        public string ResultDecision { get; set; }
    }

    public class VehicleInfoDTO
    {
        [JsonProperty(PropertyName = "vehicle")]
        public string Vehicle { get; set; }

        [JsonProperty(PropertyName = "engine")]
        public int Engine { get; set; }
    }
}
