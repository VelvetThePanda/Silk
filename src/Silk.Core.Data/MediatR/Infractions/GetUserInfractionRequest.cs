﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Silk.Core.Data.DTOs;
using Silk.Core.Data.Models;

namespace Silk.Core.Data.MediatR.Infractions
{
	public sealed record GetUserInfractionRequest(ulong UserId, ulong GuildId, InfractionType Type) : IRequest<InfractionDTO?>;

	public sealed class GetUserInfractionHandler : IRequestHandler<GetUserInfractionRequest, InfractionDTO?>
	{
		private readonly GuildContext _db;
		public GetUserInfractionHandler(GuildContext db) => _db = db;

		public async Task<InfractionDTO?> Handle(GetUserInfractionRequest request, CancellationToken cancellationToken)
		{
			var inf = await _db.Infractions
				.Where(inf => inf.UserId == request.UserId)
				.Where(inf => inf.GuildId == request.GuildId)
				.Where(inf => inf.InfractionType == request.Type)
				.OrderBy(inf => inf.CaseNumber)
				.FirstOrDefaultAsync(cancellationToken);

			if (inf is null)
				return null;
			
			return new(inf);
		}
	}
}