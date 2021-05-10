using System.Text.RegularExpressions;

namespace SimonShapesModule
{
    public static class Constants
    {
        public static readonly string[][] ShapeTable = new[]
        {
            new[] {"ddl,ruu", "ddr,luu", "drru,dllu", "rdd,uul", "rrdl,rull,dlul,rdru", "ruru,dldl"},
            new[] {"drdr,lulu", "urr,lld", "rur,ldl", "rddr,luul", "rr,ll", "uru,dld"},
            new[] {"uu,dd", "ruur,lddl", "ur,ld", "rrd,ull", "rru,dll", "rdl,dlu,lur,urd"},
            new[] {"drrd,ullu", "rdr,lul", "rd,ul", "drr,llu", "ldrr,llur,urdr,luld", "lddr,luur"},
            new[] {"dr,lu", "rdrd,ulul", "rddl,ruul", "ddlu,druu,luru,dldr", "uur,ldd", "r,l"},
            new[] {"urur,ldld", "urrd,ulld", "uurd,uldd,urul,rdld", "ru,dl", "d,u", "drd,ulu"}
        };

        public static readonly int[][] NumberTable = new[]
        {
            new[] {1, 2, 3, 4, 5, 6},
            new[] {4, 5, 6, 1, 2, 3},
            new[] {2, 3, 4, 5, 6, 1},
            new[] {6, 1, 2, 3, 4, 5},
            new[] {3, 4, 5, 6, 1, 2},
            new[] {5, 6, 1, 2, 3, 4}
        };

        public static readonly Regex TPColorRegex = new Regex("^press ([rgbymc ]+)$");
        public static readonly Regex TPCoorRegex = new Regex("^press (([abc][123])( ([abc][123]))*)$");
    }
}