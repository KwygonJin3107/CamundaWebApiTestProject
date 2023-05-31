using Zeebe.Client.Api.Responses;

namespace CamundaWebApiTestProject.Services
{
    public interface IZeebeService
    {
        public Task<ITopology> Status();
        public Task<String> StartWorkflowInstance(string bpmProcessId);
        public void StartWorkers();
    }
}
