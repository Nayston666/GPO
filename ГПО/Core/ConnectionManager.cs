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
            if (source.ToolId == target.ToolId)
                return false;

            // Если цель - порт подсистемы (TargetPortIndex >= 0 и InputType == null)
            if (target.PortIndex >= 0 && !target.InputType.HasValue)
            {
                // Удаляем только соединения, ведущие на этот же порт
                _connections.RemoveAll(c => c.TargetToolId == target.ToolId && c.TargetPortIndex == target.PortIndex);
            }
            else if (target.InputType.HasValue)
            {
                // Обычный вход A или B
                _connections.RemoveAll(c => c.TargetToolId == target.ToolId && c.TargetInput == target.InputType.Value);
            }
            else
            {
                // fallback
                _connections.RemoveAll(c => c.TargetToolId == target.ToolId);
            }

            var newConn = new Connection
            {
                Id = Guid.NewGuid(),
                SourceToolId = source.ToolId,
                TargetToolId = target.ToolId,
                TargetInput = target.InputType ?? InputType.A,
                TargetPortIndex = target.PortIndex,
                SourcePortIndex = source.PortIndex
            };

            _connections.Add(newConn);
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