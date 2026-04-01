using System;
using System.Collections.Generic;
using System.Linq;
using MathApp.Models;

namespace MathApp.Core
{
    public class ConnectionManager
    {
        private List<Connection> _connections;

        public ConnectionManager(List<Connection> connections)
        {
            _connections = connections;
        }

        public bool CreateConnection(ConnectionPoint source, ConnectionPoint target)
        {
            if (source.ToolId == target.ToolId) return false;

            _connections.RemoveAll(c => c.TargetToolId == target.ToolId && c.TargetInput == target.InputType);
            _connections.Add(new Connection
            {
                Id = Guid.NewGuid(),
                SourceToolId = source.ToolId,
                TargetToolId = target.ToolId,
                TargetInput = target.InputType.Value
            });
            return true;
        }

        public void RemoveConnectionsForTool(Guid toolId)
        {
            _connections.RemoveAll(c => c.SourceToolId == toolId || c.TargetToolId == toolId);
        }

        public void RemoveIncomingConnections(Guid toolId)
        {
            _connections.RemoveAll(c => c.TargetToolId == toolId);
        }
    }
}