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

            // Для подсистемы: удаляем старое соединение для КОНКРЕТНОГО порта
            _connections.RemoveAll(c =>
                c.TargetToolId == target.ToolId &&
                c.TargetInput == target.InputType &&
                c.TargetPortIndex == target.PortIndex);  // Учитываем индекс порта

            // Создаем новое
            var newConn = new Connection
            {
                Id = Guid.NewGuid(),
                SourceToolId = source.ToolId,
                TargetToolId = target.ToolId,
                TargetInput = target.InputType ?? InputType.A,
                TargetPortIndex = target.PortIndex, // Сохраняем индекс порта входа
                SourcePortIndex = source.PortIndex  // Сохраняем индекс порта выхода
            };

            _connections.Add(newConn);

            System.Diagnostics.Debug.WriteLine($"Created connection: Source={source.ToolId}, Target={target.ToolId}, PortIndex={target.PortIndex}");

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