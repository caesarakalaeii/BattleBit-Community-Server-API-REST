using System.Collections.Concurrent;
using System.Numerics;
using BattleBitAPI.Common;
using Newtonsoft.Json;

namespace CommunityServerAPI;

public class Command
{
    public ActionType Action { get; set; }
    public ulong StreamerId { get; set; }
    public int Amount { get; set; }

    public Vector3 Location { get; set; }
    public IEnumerable<string> Data { get; set; }

    public string ExecutorName { get; set; }

    public List<Attachment> AttachmentChange { get; set; }

    public List<Weapon> WeaponChange { get; set; }

    public List<Gadget> GadgetChange { get; set; }
}

public class CommandQueue
{
    private readonly ConcurrentQueue<Command> mQueue = new();

    public void Enqueue(Command command)
    {
        mQueue.Enqueue(command);
    }

    public Command Dequeue()
    {
        mQueue.TryDequeue(out var command);
        return command;
    }

    public bool IsEmpty()
    {
        return mQueue.IsEmpty;
    }
}

internal class RESTListener
{
    private static readonly HttpClient Client = new();
    public static readonly CommandQueue Queue = new();

    private static void Run()
    {
        while (true) PollData().Wait();
    }

    private static async Task PollData()
    {
        //This is used to consume rest API
        var response = await Client.GetAsync("http://my-api-url/data");

        //Throw exception if not successful
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        //Deserialize response body into DataObject object
        var command = JsonConvert.DeserializeObject<Command>(responseBody);

        //Enqueue it into queue
        Queue.Enqueue(command);
    }
}