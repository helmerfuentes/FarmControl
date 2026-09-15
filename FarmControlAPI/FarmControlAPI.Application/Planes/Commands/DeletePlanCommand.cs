using FarmControlAPI.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmControlAPI.Application.Planes.Commands;

public record DeletePlanCommand(int Id) : IRequest<Result<bool>>;

public class DeletePlanCommandHandler : IRequestHandler<DeletePlanCommand, Result<bool>>
{
	private readonly IFarmControlDbContext _context;
	private readonly IBitacoraService _bitacora;

	public DeletePlanCommandHandler(IFarmControlDbContext context, IBitacoraService bitacora)
	{
		_context = context;
		_bitacora = bitacora;
	}

	public async Task<Result<bool>> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
	{
		Result<bool> result;

		var plan = await _context.Planes.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
		if (plan is null)
		{
			result = Result<bool>.Failure($"Plan con Id {request.Id} no encontrado.");
			return result;
		}

		var clientesConEstePlan = await _context.Clientes.AnyAsync(c => c.PlanId == request.Id, cancellationToken);
		if (clientesConEstePlan)
		{
			result = Result<bool>.Failure("No se puede eliminar el plan porque hay clientes asignados a él.");
			return result;
		}

		_context.Planes.Remove(plan);
		await _context.SaveChangesAsync(cancellationToken);

		await _bitacora.RegistrarAsync("Plan eliminado", $"Se eliminó el plan '{plan.Nombre}'.", cancellationToken: cancellationToken);

		result = Result<bool>.Success(true);
		return result;
	}
}
