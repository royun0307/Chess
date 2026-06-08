using System.Collections.Generic;
using System.Linq;

// 폰(앞으로 전진하고 대각선으로 잡으며, 프로모션과 앙파상이 가능한 체스 기물) 클래스
public class Pawn : Piece
{
    // 이 기물의 종류는 Pawn
    public override PieceType Type => PieceType.Pawn;
    
    // 이 기물의 색상(흑/백)
    public override PlayerColor Color { get; }

    // 폰이 전진하는 방향
    // 백은 위쪽(North), 흑은 아래쪽(South)으로 이동
    private readonly Direction forward;

    // 폰 생성자
    // 생성할 때 기물의 색상을 받아 저장하고 전진 방향을 설정 
    public Pawn(PlayerColor color)
    {
        this.Color = color;

        if (color == PlayerColor.White)
        { 
            forward = Direction.North;
        }
        else if (color == PlayerColor.Black) 
        {
            forward = Direction.South;
        }
    }
    
    // 현재 폰 객체를 복사해서 새로운 폰 객체를 반환
    public override Piece Copy()
    {
        // 같은 색상의 폰 생성
        Pawn copy = new Pawn(Color);
        
        // 이동 여부(hasMoved)도 함꼐 복사
        copy.hasMoved = hasMoved;

        return copy;
    }

    // 해당 위치로 전진할 수 있는지 확인
    // 보드 안에 있으면서 빈 칸이어야 한다
    private static bool CanMoveTo(Position pos, Board board)
    { 
        return Board.IsInside(pos) && board.IsEmpty(pos);
    }

    // 해당 위치의 기물을 대각선으로 잡을 수 있는지 확인
    // 보드 안에 있고, 비어 있지 않으며, 상대 기물이어야 한다.
    private bool CanCaptureAt(Position pos, Board board)
    { 
        if(!Board.IsInside(pos) || board.IsEmpty(pos))
        {
            return false;
        }

        return board[pos].Color != Color;
    }

    // 프로모션 가능한 모든 이동을 생성
    // 폰이 마지막 줄에 도달했을 때 나이트, 비숍, 룩, 퀸으로 승급 가능
    private static IEnumerable<Move> PromotionMoves(Position from, Position to)
    {
        yield return new PawnPromotion(from, to, PieceType.Knight);
        yield return new PawnPromotion(from, to, PieceType.Bishop);
        yield return new PawnPromotion(from, to, PieceType.Rook);
        yield return new PawnPromotion(from, to, PieceType.Queen);
    }

    // 폰의 전진 이동들을 계산
    // 한 칸 전진, 시작 위치에서 두 칸 전진, 전진 후 프로모션을 처리
    private IEnumerable<Move> ForwardMoves(Position from, Board board)
    {
        Position one_move_pos = from + forward;

        // 한 칸 앞이 비어 있으면 전진 가능
        if(CanMoveTo(one_move_pos, board))
        {
            // 마지막 줄이 도달하면 일반 이동이 아니라 프로모션 이동 생성
            if(one_move_pos.row == 0 || one_move_pos.row == 7)
            {
                foreach(Move proMove in PromotionMoves(from, one_move_pos))
                {
                    yield return proMove;
                }
            }
            else
            {
                yield return new NormalMove(from, one_move_pos);
            }

            // 아직 움지이지 않은 폰이면 두 칸 전진도 가능
            Position two_move_pos = one_move_pos + forward;

            if(!hasMoved && CanMoveTo(two_move_pos, board))
            {
                yield return new DoublePawn(from, two_move_pos);
            }
        }
    }
    
    // 폰의 대각선 이동들을 계산
    // 일반 잡기, 프로모션 잡기, 앙파상을 처리
    private IEnumerable<Move> DiagonalMoves(Position from, Board board) 
    {
        foreach(Direction dir in new Direction[] { Direction.West, Direction.East })
        {
            Position to = from + forward + dir;

            // 상대가 직전에 두 칸 전진한 폰을 잡을 수 있는 위치라면 앙파상 가능
            if (to == board.GetPawnSkipPosition(Color.Opponent()))
            {
                yield return new Enpassant(from, to);
            }
            // 일반적인 대각선 잡기 가능 여부 확인
            else if(CanCaptureAt(to, board))
            {
                // 마지막 줄에서 잡는 경우 프로모션 이동 생성
                if (to.row == 0 || to.row == 7)
                {
                    foreach (Move proMove in PromotionMoves(from, to))
                    {
                        yield return proMove;
                    }
                }
                else
                {
                    yield return new NormalMove(from, to);
                }
            }
        }
    }

    // 현재 위치(from)에서 이동 가능한 모든 수를 반환
    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        // 전진 이동과 대각선 이동을 합쳐서 반환
        return ForwardMoves(from, board).Concat(DiagonalMoves(from, board));
    }

    // 현재 폰이 상대 킹을 공격할 수 있는지 확인
    public override bool CanCaptureOpponentKing(Position from, Board board)
    {
        return DiagonalMoves(from, board).Any(move =>
        {
            Piece piece = board[move.ToPos];
            return piece != null && piece.Type == PieceType.King;
        });
    }
}