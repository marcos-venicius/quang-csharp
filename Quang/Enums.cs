namespace Quang;

internal enum TokenKind
{
	OpenParen,
	CloseParen,
	AndKeyword,
	OrKeyword,
	NilKeyword,
	EqKeyword,
	NeKeyword,
	GtKeyword,
	LtKeyword,
	GteKeyword,
	LteKeyword,
	RegKeyword,

	Symbol,

	Integer,
	Atom,
	String,
	Float,

	TrueKeyword,
	FalseKeyword,
}
