public class GameMode : MyGameServer
{
    public string Name = string.Empty;
    protected MyGameServer R;

    protected GameMode(MyGameServer r)
    {
        R = r;
    }

    public virtual void Init()
    {
    }

    public virtual void Reset()
    {
        foreach (var player in R.AllPlayers) player.Kill();
    }

    public override Task OnRoundEnded()
    {
        Reset();
        return base.OnRoundEnded();
    }
}