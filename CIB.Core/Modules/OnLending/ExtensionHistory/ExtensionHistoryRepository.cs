
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.OnLending.Transaction;

namespace CIB.Core.Modules.OnLending.ExtensionHistory
{
	public class ExtensionHistoryRepository : Repository<TblOnlendingExtensionHistory>, IExtensionHistoryRepository
	{
		public ExtensionHistoryRepository(ParallexCIBContext context) : base(context)
		{
		}

		public ParallexCIBContext context
		{
			get { return _context as ParallexCIBContext; }
		}
		
	}
}

