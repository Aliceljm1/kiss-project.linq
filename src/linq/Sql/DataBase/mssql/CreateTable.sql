IF Not EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[#ENTITY#]') AND type in (N'U'))
CREATE TABLE [dbo].[#ENTITY#]
(
	#PARAMS#
)ON [PRIMARY];