using System.Threading.Tasks;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Dao;

public interface ITpqMessagesDao
{
    void Insert(TpqCreateMessageModel message);
    Task InsertAsync(TpqCreateMessageModel message);
}