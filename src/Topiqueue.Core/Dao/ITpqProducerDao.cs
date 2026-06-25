using System.Threading.Tasks;
using Topiqueue.Core.Messages.Models;

namespace Topiqueue.Core.Dao;

public interface ITpqProducerDao
{
    void Insert(TpqCreateMessageModel message);
    Task InsertAsync(TpqCreateMessageModel message);
}