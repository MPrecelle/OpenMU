// <copyright file="ShowLockedContentPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.Properties;

/// <summary>
/// Sends the locked content to the game client with the <see cref="LockedContentRef"/> packet, a custom packet of Torvik MU.
/// Clients which don't know it ignore it.
/// </summary>
[PlugIn]
[Display(Name = nameof(ContentStageResources.ShowLockedContentPlugIn_Name), Description = nameof(ContentStageResources.ShowLockedContentPlugIn_Description), ResourceType = typeof(ContentStageResources))]
[Guid("3C8E5A21-7B4D-4F6A-9E12-5D0B8C7A6F34")]
public sealed class ShowLockedContentPlugIn : IShowLockedContentPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowLockedContentPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowLockedContentPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowLockedContentAsync(LockedClientContent content)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        // The count is a byte; there are far fewer maps.
        var maps = content.LockedMaps.Take(byte.MaxValue).ToList();
        int Write()
        {
            var size = LockedContentRef.GetRequiredSize(maps.Count);
            var span = connection.Output.GetSpan(size)[..size];
            var packet = new LockedContentRef(span)
            {
                IsGensLocked = content.IsGensLocked,
                IsCashShopLocked = content.IsCashShopLocked,
                LockedMapCount = (byte)maps.Count,
            };

            for (var i = 0; i < maps.Count; i++)
            {
                var map = packet[i];
                map.MapNumber = (ushort)maps[i];
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
