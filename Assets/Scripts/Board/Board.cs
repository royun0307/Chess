using System;
using System.Collections.Generic;
using System.Linq;

public class Board
{
    // 체스판(8x8). 각 칸에 Piece 또는 null 저장
    private readonly Piece[,] pieces = new Piece[8, 8];

    // 앙파상(En Passant) 가능 판정을 위해,
    // 각 플레이어 기준 "상대가 직전에 2칸 전진한 폰의 위치"를 저장
    // ex) White 차례에서 Black의 skip position 확인
    private readonly Dictionary<PlayerColor, Position> pawn_skip_postions = new Dictionary<PlayerColor, Position>
    {
        {PlayerColor.White, null },
        {PlayerColor.Black, null }
    };

    // [row, col] 인덱서로 말 접근 가능
    public Piece this[int row, int col]
    {
        get { return pieces[row, col]; }
        set { pieces[row, col] = value; }
    }

    // [Position] 인덱서로 말 접근 가능
    public Piece this[Position pos]
    {
        get { return pieces[pos.row, pos.column]; }
        set { pieces[pos.row, pos.column] = value; }
    }

    // 특정 플레이어 기준 저장된 pawn skip 위치 반환
    public Position GetPawnSkipPosition(PlayerColor player)
    {
        return pawn_skip_postions[player];
    }

    // 특정 플레이어 기준 pawn skip 위치 설정
    public void SetPawnSkipPosition(PlayerColor player, Position pos)
    {
        pawn_skip_postions[player] = pos;
    }

    // 초기 배치된 새 보드 생성
    public static Board Initial()
    {
        Board board = new Board();
        board.AddStartPieces();
        return board;
    }

    // 시작 기물 배치
    private void AddStartPieces()
    {
        // 흑 기물 초기 배치
        this[0, 0] = new Rook(PlayerColor.Black);
        this[0, 1] = new Knight(PlayerColor.Black);
        this[0, 2] = new Bishop(PlayerColor.Black);
        this[0, 3] = new Queen(PlayerColor.Black);
        this[0, 4] = new King(PlayerColor.Black);
        this[0, 5] = new Bishop(PlayerColor.Black);
        this[0, 6] = new Knight(PlayerColor.Black);
        this[0, 7] = new Rook(PlayerColor.Black);

        // 백 기물 초기 배치
        this[7, 0] = new Rook(PlayerColor.White);
        this[7, 1] = new Knight(PlayerColor.White);
        this[7, 2] = new Bishop(PlayerColor.White);
        this[7, 3] = new Queen(PlayerColor.White);
        this[7, 4] = new King(PlayerColor.White);
        this[7, 5] = new Bishop(PlayerColor.White);
        this[7, 6] = new Knight(PlayerColor.White);
        this[7, 7] = new Rook(PlayerColor.White);

        // 폰 초기 배치
        for (int i = 0; i < 8; i++)
        {
            this[1, i] = new Pawn(PlayerColor.Black);
            this[6, i] = new Pawn(PlayerColor.White);
        }
    }

    // 보드 내부 좌표인지 검사
    public static bool IsInside(Position pos)
    {
        return pos.row >= 0 && pos.row < 8 && pos.column >= 0 && pos.column < 8;
    }

    // 해당 위치가 비어있는지 여부
    public bool IsEmpty(Position pos)
    {
        return this[pos] == null;
    }

    // 현재 보드에 존재하는 모든 기물의 위치를 반환
    public IEnumerable<Position> PiecePositions()
    {
        for (int r = 0; r < 8; r++)
        {
            for(int c = 0; c < 8; c++)
            {
                Position pos = new Position(r, c);

                if (!IsEmpty(pos))
                {
                    yield return pos;
                }
            }
        }
    }

    // 특정 플레이어의 기물 위치만 반환
    public IEnumerable<Position> PiecePositionsFor(PlayerColor player)
    {
        return PiecePositions().Where(pos => this[pos].Color == player);
    }

    // player가 체크 상태인지 확인
    // 상대 기물 중 하나라도 player의 킹을 잡을 수 있으면 true
    public bool IsInCheck(PlayerColor player)
    {
        return PiecePositionsFor(player.Opponent()).Any(pos =>
        {
            Piece piece = this[pos];
            return piece.CanCaptureOpponentKing(pos, this);
        });
    }

    // 현재 보드를 깊은 복사
    public Board Copy()
    {
        Board copy = new Board();

        // 각 기물을 Copy()로 복제
        foreach (Position pos in PiecePositions())
        {
            copy[pos] = this[pos].Copy();
        }

        // 앙파상 관련 상태도 함께 복사
        copy.SetPawnSkipPosition(PlayerColor.White, pawn_skip_postions[PlayerColor.White]);
        copy.SetPawnSkipPosition(PlayerColor.Black, pawn_skip_postions[PlayerColor.Black]);

        return copy;
    }

    // 보드 위 기물 개수/종류를 카운팅해서 반환
    public Counting CountPieces()
    {
        Counting counting = new Counting();

        foreach(Position pos in PiecePositions())
        {
            Piece piece = this[pos];
            counting.Increment(piece.Color, piece.Type);
        }

        return counting;
    }

    // 불충분한 기물(무승부) 판정
    public bool InsufficientMaterial()
    {
        Counting counting = CountPieces();

        return IsKingVKing(counting) || IsKingBishopVKing(counting) ||
            IsKingKnightVKing(counting) || IsKingBishopVKingBishop(counting);
    }

    // 킹 vs 킹
    private bool IsKingVKing(Counting counting)
    {
        return counting.TotalCount == 2;
    }

    // 킹+비숍 vs 킹
    private bool IsKingBishopVKing(Counting counting)
    {
        return counting.TotalCount == 3 && (counting.GetWhiteCount(PieceType.Bishop) == 1 || counting.GetBlackCount(PieceType.Bishop) == 1);
    }

    // 킹+나이트 vs 킹
    private bool IsKingKnightVKing(Counting counting)
    {
        return counting.TotalCount == 3 && (counting.GetWhiteCount(PieceType.Knight) == 1 || counting.GetBlackCount(PieceType.Knight) == 1);
    }

    // 킹+비숍 vs 킹+비숍 (둘 다 비숍 1개씩, 같은 색 칸 비숍이면 불충분 기물)
    private bool IsKingBishopVKingBishop(Counting counting)
    {
        if(counting.TotalCount != 4)
        {
            return false;
        }

        if(counting.GetWhiteCount(PieceType.Bishop) != 1 || counting.GetBlackCount(PieceType.Bishop) != 1)
        {
            return false;
        }

        Position w_bishop_pos = FindPiece(PlayerColor.White, PieceType.Bishop);
        Position b_bishop_pos = FindPiece(PlayerColor.Black, PieceType.Bishop);

        // 비숍이 같은 색 칸만 다니는 경우 체크메이트 불가 → 무승부 판정
        return w_bishop_pos.SquareColor() == b_bishop_pos.SquareColor();
    }

    // 특정 색/타입의 기물 위치 찾기 (첫 번째 것)
    private Position FindPiece(PlayerColor color, PieceType type)
    {
        return PiecePositionsFor(color).First(pos => this[pos].Type == type);
    }

    // 캐슬링 권리 판정용:
    // 해당 위치에 킹/룩이 있고, 둘 다 아직 이동하지 않았는지 확인
    private bool IsUnmovedKingAndRook(Position king_pos, Position rook_pos)
    {
        if(IsEmpty(king_pos) || IsEmpty(rook_pos))
        {
            return false;
        }

        Piece king = this[king_pos];
        Piece rook = this[rook_pos];

        return king.Type == PieceType.King && rook.Type == PieceType.Rook && !king.hasMoved && !rook.hasMoved;
    }

    // 킹사이드 캐슬링 권리 여부 (말의 이동 여부 기준)
    // 실제 캐슬링 가능 여부(중간 칸 비었는지/체크 통과 여부 등)는 별도 move legality에서 검사해야 함
    public bool CastleRightKS(PlayerColor player)
    {
        return player switch
        {
            PlayerColor.White => IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 7)),
            PlayerColor.Black => IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 7)),
            _ => false
        };
    }

    // 퀸사이드 캐슬링 권리 여부 (말의 이동 여부 기준)
    public bool CastleRightQS(PlayerColor player)
    {
        return player switch
        {
            PlayerColor.White => IsUnmovedKingAndRook(new Position(7, 4), new Position(7, 0)),
            PlayerColor.Black => IsUnmovedKingAndRook(new Position(0, 4), new Position(0, 0)),
            _ => false
        };
    }

    // 주어진 후보 위치들 중에서 실제로 앙파상 가능한 폰이 있는지 검사
    private bool HasPawnInPosition(PlayerColor player, Position[] pawn_positinos, Position skip_pos)
    {
        foreach (Position pos in pawn_positinos.Where(IsInside))
        {
            Piece piece = this[pos];

            // 플레이어의 폰이 아니면 스킵
            if (piece == null || piece.Color != player || piece.Type != PieceType.Pawn)
            {
                continue;
            }

            // 해당 폰이 skip_pos로 앙파상 가능한지 Move 객체로 판정
            Enpassant move = new Enpassant(pos, skip_pos);
            if (move.IsLegal(this))
            {
                return true;
            }
        }

        return false;
    }

    // 현재 플레이어가 앙파상 캡처를 할 수 있는지 여부
    public bool CanCaptureEnPassant(PlayerColor player)
    {
        // 상대가 직전에 2칸 전진한 폰의 위치를 가져옴
        Position skip_pos = GetPawnSkipPosition(player.Opponent());

        // 그런 위치가 없으면 앙파상 불가
        if (skip_pos == null)
        {
            return false;
        }

        // skip_pos를 기준으로, 앙파상 가능한 내 폰의 후보 위치 계산
        Position[] pawn_positions = player switch
        {
            PlayerColor.White => new Position[] {skip_pos + Direction.SouthWest, skip_pos + Direction.SouthEast},
            PlayerColor.Black => new Position[] {skip_pos + Direction.NorthWest, skip_pos + Direction.NorthEast},
            _ => Array.Empty<Position>()
        };

        return HasPawnInPosition(player, pawn_positions, skip_pos);
    }
}
