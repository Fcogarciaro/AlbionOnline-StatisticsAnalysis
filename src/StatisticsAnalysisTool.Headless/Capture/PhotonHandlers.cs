using System;
using System.Threading.Tasks;
using Serilog;
using StatisticsAnalysisTool.Abstractions;
using StatisticsAnalysisTool.Headless.Events;
using StatisticsAnalysisTool.Headless.Mapping;
using StatisticsAnalysisTool.Network;

namespace StatisticsAnalysisTool.Headless.Capture;

internal sealed class MappedEventHandler<TEvent>(EventCodes code, Action<TEvent> action) : EventPacketHandler<TEvent>((int) code)
{
    protected override Task OnActionAsync(TEvent value)
    {
        try
        {
            action(value);
        }
        catch (Exception e)
        {
            Log.Error(e, "Handler for event {Code} failed", code);
        }

        return Task.CompletedTask;
    }
}

internal sealed class MappedResponseHandler<TOperation>(OperationCodes code, Action<TOperation> action) : ResponsePacketHandler<TOperation>((int) code)
{
    protected override Task OnActionAsync(TOperation value)
    {
        try
        {
            action(value);
        }
        catch (Exception e)
        {
            Log.Error(e, "Handler for response {Code} failed", code);
        }

        return Task.CompletedTask;
    }
}

internal static class PhotonReceiverFactory
{
    /// <summary>Builds the Photon receiver that feeds the mapper. Only the events the contract needs are registered.</summary>
    public static IPhotonReceiver Build(EventMapper mapper)
    {
        var builder = ReceiverBuilder.Create();

        builder.AddEventHandler(new MappedEventHandler<FameInfo>(EventCodes.UpdateFame, mapper.OnFame));
        builder.AddEventHandler(new MappedEventHandler<TakeSilverInfo>(EventCodes.TakeSilver, mapper.OnTakeSilver));
        builder.AddEventHandler(new MappedEventHandler<ReSpecInfo>(EventCodes.UpdateReSpecPoints, mapper.OnReSpecPoints));
        builder.AddEventHandler(new MappedEventHandler<MightFavorInfo>(EventCodes.MightAndFavorReceivedEvent, mapper.OnMightAndFavor));
        builder.AddEventHandler(new MappedEventHandler<DiedInfo>(EventCodes.Died, mapper.OnDied));
        builder.AddEventHandler(new MappedEventHandler<NewLootChestInfo>(EventCodes.NewLootChest, mapper.OnNewLootChest));
        builder.AddEventHandler(new MappedEventHandler<UpdateLootChestInfo>(EventCodes.UpdateLootChest, mapper.OnUpdateLootChest));
        builder.AddEventHandler(new MappedEventHandler<GrabbedLootInfo>(EventCodes.OtherGrabbedLoot, mapper.OnGrabbedLoot));

        builder.AddResponseHandler(new MappedResponseHandler<JoinInfo>(OperationCodes.Join, mapper.OnJoin));
        builder.AddResponseHandler(new MappedResponseHandler<ChangeClusterInfo>(OperationCodes.ChangeCluster, mapper.OnChangeCluster));

        return builder.Build();
    }
}
