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

        /// <summary>
        /// Создает новое соединение между блоками
        /// </summary>
        public bool CreateConnection(ConnectionPoint source, ConnectionPoint target)
        {
            // Нельзя соединять блок сам с собой
            if (source.ToolId == target.ToolId)
                return false;

            // Удаляем старое соединение для этого входа
            _connections.RemoveAll(c =>
                c.TargetToolId == target.ToolId &&
                c.TargetInput == target.InputType);

            // Создаем новое
            _connections.Add(new Connection
            {
                Id = Guid.NewGuid(),
                SourceToolId = source.ToolId,
                TargetToolId = target.ToolId,
                TargetInput = target.InputType.Value
            });

            return true;
        }

        /// <summary>
        /// Удаляет все соединения, связанные с блоком
        /// </summary>
        public void RemoveConnectionsForTool(Guid toolId)
        {
            _connections.RemoveAll(c =>
                c.SourceToolId == toolId || c.TargetToolId == toolId);
        }

        /// <summary>
        /// Удаляет все входящие соединения для блока
        /// </summary>
        public void RemoveIncomingConnections(Guid toolId)
        {
            _connections.RemoveAll(c => c.TargetToolId == toolId);
        }
    }
}