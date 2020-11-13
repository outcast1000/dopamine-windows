namespace Dopamine.Data
{
    public enum AddFolderResult
    {
        Error = 0,
        Success = 1,
        Duplicate = 2,
        Inaccessible = 3
    }

    public enum RemoveFolderResult
    {
        Error = 0,
        Success = 1
    }

    public enum TrackOrder
    {
        Alphabetical,
        ReverseAlphabetical,
        ByAlbum,
        ByFileName,
        ByRating,
        Ranking,
        Random,
        None
    }

    public enum AlbumOrder
    {
        AlphabeticalAscending,
        AlphabeticalDescending,
        ByAlbumArtistAscending,
        ByAlbumArtistDescending,
        ByDateAdded,
        ByDateCreated,
        ByTrackCount,
        ByYearDescending,
        ByYearAscending,
        ByPlayCount
    }

    public enum ArtistOrder
    {
        AlphabeticalAscending,
        AlphabeticalDescending,
        ByDateAdded ,
        ByDateCreated,
        ByTrackCount,
        ByYearDescending,
        ByYearAscending,
        ByPlayCount
    }

    public enum GenreOrder
    {
        AlphabeticalAscending,
        AlphabeticalDescending,
        ByTrackCount,
        ByPlayCount
    }



    public enum RemoveTracksResult
    {
        Error = 0,
        Success = 1
    }
}
